using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace hammertime;

public enum PlayerState
{
    DEAD,
    FALLING,
    STANDING,
    WALKING,
    PUSHBACK,
    CHARGING,
    DASHING,
    IMMOBILIZED
}

public abstract class UnstoppableMove
{
    public UnstoppableMove(Vector3 direction, float distance, float velocity)
    {
        Direction = direction;
        Distance = distance;
        Velocity = velocity;
    }
    public Vector3 Direction { get; }
    public float Distance { get; set; }
    public float Velocity { get; }
};

public class Pushback : UnstoppableMove
{
    public Pushback(Vector3 direction, float distance, float velocity) : base(direction, distance, velocity)
    {

    }

    // how far one gets pushed back by a hammer
    public const float PushbackDistance = 3f;
    public const float PushbackVelocity = 2000f;
}

public class Dash : UnstoppableMove
{
    public Dash(Vector3 direction, float distance, float velocity) : base(direction, distance, velocity)
    {

    }

    // how far a player can dash
    public const float DashDistance = 3f;
    public const float DashVelocity = 8000f;
}

public class Player : GameObject<PlayerState>
{

    // player attributes
    public int PlayerId { get => _playerId; }
    private int _playerId;

    private Vector3 _velocity;

    // TODO (fbuetler) if a player is smaller than a tile it is immediately falling upon start
    public override Vector3 MaxSize { get => _maxSize; }
    private static Vector3 _maxSize = new Vector3(1.5f, 1.5f, 1.5f);

    public override PlayerState State { get => _state; }
    private PlayerState _state;

    // charge
    public float ThrowDistance { get => Math.Clamp(_chargeDurationMs * ChargeUnit, Hammer.MinThrowDistance, Hammer.MaxThrowDistance); }
    private float _chargeDurationMs;

    // note: this is null when we're not in a pushback state
    private Pushback _pushback;

    // note: this is null when we're not in a dashing state
    private Dash _dash;

    private float _remainingImmobilizedDuration = 0f;

    private Dictionary<PlayerState, string> _objectModelPaths;
    public override Dictionary<PlayerState, string> ObjectModelPaths => _objectModelPaths;

    // sound effects
    private int _stepIndex = 0;
    private float _timeSinceLastStepMs = 0;

    // if a player is below the kill plane, it disappears
    public const float KillPlaneLevel = -10f;

    // charge/throw
    private const float ChargeUnit = 0.02f;

    // dash
    private const float MinDashDistance = 0.5f;

    // immobilized
    private const float ImmobilizedDurationMs = 500;

    // constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveVelocity = 16f;
    private const float GroundDragFactor = 0.48f;

    // constants for controlling vertical movement
    private const float GravityAcceleration = 200f;
    private const float MaxFallVelocity = 20f;
    private const float AirDragFactor = 0.58f;

    // sound effects
    private const string HammerHitSoundEffect = "HammerAudio/hammerBong";
    private const string PlayerFallingSoundEffect = "MouvementAudio/falling";
    private const string DashSoundEffect = "MouvementAudio/Dash";
    private const string StepSoundEffectPrefix = "MouvementAudio/step";

    private const int NumStepSoundEffects = 10;
    private const int StepSoundEffectIntervalMs = 350;

    public Player(Game game, Vector3 position, int playerId) : base(game, position + _maxSize / 2)
    {
        // make update and draw called by monogame
        Enabled = false; // enabled by match
        UpdateOrder = GameMain.PLAYER_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.PLAYER_DRAW_ORDER;

        _playerId = playerId;

        _state = PlayerState.STANDING;

        _objectModelPaths = new Dictionary<PlayerState, string>();
        _objectModelPaths[PlayerState.STANDING] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.WALKING] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.PUSHBACK] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.FALLING] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.DEAD] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.DASHING] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.CHARGING] = $"Player/playerNoHammer_{playerId}";
        _objectModelPaths[PlayerState.IMMOBILIZED] = $"Player/playerNoHammer_{playerId}";
    }

    protected override void LoadAudioContent()
    {
        GameMain.AudioManager.LoadSoundEffect(PlayerFallingSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(HammerHitSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(DashSoundEffect);
        for (int i = 0; i < NumStepSoundEffects; i++)
        {
            GameMain.AudioManager.LoadSoundEffect($"{StepSoundEffectPrefix}{i}");
        }
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 moveInput = ReadMovementInput();
        Vector3 prevCenter = Center;

        switch (State)
        {
            case PlayerState.STANDING when Controls.Throw(_playerId).Held() && GameMain.Match.Map.Hammers[_playerId].State == HammerState.IS_HELD:
            case PlayerState.WALKING when Controls.Throw(_playerId).Held() && GameMain.Match.Map.Hammers[_playerId].State == HammerState.IS_HELD:
                _chargeDurationMs = 0;
                _state = PlayerState.CHARGING;
                break;
            case PlayerState.WALKING when Controls.Dash(_playerId).Pressed():
                float distance = CalculateSmartDashDistance();
                _dash = new Dash(Direction, distance, Dash.DashVelocity);
                _state = PlayerState.DASHING;
                Visible = false;
                GameMain.AudioManager.PlaySoundEffect(DashSoundEffect);
                break;
            case PlayerState.STANDING when moveInput != Vector3.Zero:
                _state = PlayerState.WALKING;
                break;
            case PlayerState.STANDING:
                _velocity = ApplyGravity(gameTime, _velocity);
                Move(gameTime, _velocity);
                break;
            case PlayerState.WALKING when moveInput == Vector3.Zero:
                _state = PlayerState.STANDING;
                break;
            case PlayerState.WALKING:
                Direction = moveInput;
                _velocity = ApplyGravity(gameTime, _velocity);
                _velocity = ComputeAcceleratedVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                Move(gameTime, _velocity);
                PlayStepSoundEffect(gameTime);
                break;
            case PlayerState.CHARGING when Controls.Throw(_playerId).Released():
                GameMain.Match.Map.Hammers[_playerId].Throw(ThrowDistance);
                _state = PlayerState.STANDING;
                break;
            case PlayerState.CHARGING:
                // move
                if (moveInput != Vector3.Zero)
                {
                    Direction = moveInput;
                    _velocity = ApplyGravity(gameTime, _velocity);
                    _velocity = ComputeAcceleratedVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                    Move(gameTime, _velocity);
                }

                // charge
                _chargeDurationMs += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                break;
            case PlayerState.DASHING when _dash.Distance <= 0:
                _dash = null;
                _state = PlayerState.IMMOBILIZED;
                _remainingImmobilizedDuration = ImmobilizedDurationMs;
                Visible = true;
                break;
            case PlayerState.DASHING:
                // TODO (fbuetler) move might go to far for a fixed distance if too much time elapsed 
                _velocity = ComputeConstantVelocity(_velocity, _dash.Direction, _dash.Velocity, GroundDragFactor, gameTime);
                _dash.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.IMMOBILIZED when _remainingImmobilizedDuration <= 0:
                _remainingImmobilizedDuration = 0;
                _state = PlayerState.STANDING;
                break;
            case PlayerState.IMMOBILIZED:
                _remainingImmobilizedDuration -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                _velocity = ApplyGravity(gameTime, _velocity);
                Move(gameTime, _velocity);
                break;
            case PlayerState.PUSHBACK when _pushback.Distance <= 0:
                _pushback = null;
                _state = PlayerState.STANDING;
                break;
            case PlayerState.PUSHBACK:
                _velocity = ApplyGravity(gameTime, _velocity);
                _velocity = ComputeConstantVelocity(_velocity, _pushback.Direction, _pushback.Velocity, GroundDragFactor, gameTime);
                _pushback.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.FALLING when Center.Y < KillPlaneLevel:
                _state = PlayerState.DEAD;
                OnKilled();
                break;
            case PlayerState.FALLING:
                _velocity = ApplyGravity(gameTime, _velocity);
                if (moveInput != Vector3.Zero)
                {
                    Direction = moveInput;
                    _velocity = ComputeAcceleratedVelocity(_velocity, Direction, MoveAcceleration, AirDragFactor, gameTime);
                }
                Move(gameTime, _velocity);
                break;
            default:
                // do nothing
                break;
        }

        HandlePlayerCollisions();
        HandleTileCollisions();

        Pushback p = CheckHammerCollisions();
        if (p != null && _pushback == null)
        {
            _pushback = (Pushback)p;
            _state = PlayerState.PUSHBACK;
            Visible = true; // in case previous state was dashing
        }

        // if collision prevented us from moving, reset velocity
        if (prevCenter.X == Center.X)
            _velocity.X = 0;
        if (prevCenter.Y == Center.Y)
            _velocity.Y = 0;
        if (prevCenter.Z == Center.Z)
            _velocity.Z = 0;

        if (IsFalling(Vector3.Zero))
        {
            // Vertical velocity means we're falling :(
            if (State != PlayerState.DASHING && State != PlayerState.FALLING && State != PlayerState.DEAD)
            {
                _state = PlayerState.FALLING;
                OnFalling();
            }
        }
    }

    private Vector3 ReadMovementInput()
    {
        Vector3 movement = Vector3.Zero;

        // get analog movement
        Vector2 m = Controls.Move(_playerId);
        movement.X = m.X;
        movement.Z = m.Y;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        movement.Z *= -1;

        // ignore small movements to prevent running in place
        if (movement.Length() < 0.3f)
            movement = Vector3.Zero;

        // if any digital horizontal movement input is found, override the analog movement
        if (Controls.MoveUp(_playerId).Held())
        {
            movement.Z -= 1.0f;
        }
        else if (Controls.MoveDown(_playerId).Held())
        {
            movement.Z += 1.0f;
        }

        if (Controls.MoveLeft(_playerId).Held())
        {
            movement.X -= 1.0f;
        }
        else if (Controls.MoveRight(_playerId).Held())
        {
            movement.X += 1.0f;
        }

        // prevent the player from running faster than his top speed
        if (movement.LengthSquared() > 1)
        {
            movement.Normalize();
        }

        return movement;
    }

    private Vector3 ComputeAcceleratedVelocity(Vector3 currentVelocity, Vector3 direction, float acceleration, float dragFactor, GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 velocity = currentVelocity + direction * acceleration * elapsed;

        // prevent player from walking faster than max move velocity (does not change vertical velocity)
        Vector2 horizontalVelocity = new Vector2(velocity.X, velocity.Z);
        if (horizontalVelocity.Length() > MaxMoveVelocity)
        {
            horizontalVelocity.Normalize();
            velocity.X = horizontalVelocity.X;
            velocity.Z = horizontalVelocity.Y;
        }

        velocity.X *= dragFactor;
        velocity.Z *= dragFactor;

        return velocity;
    }

    private Vector3 ComputeConstantVelocity(Vector3 currentVelocity, Vector3 direction, float constantVelocity, float dragFactor, GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 velocity = direction * constantVelocity * elapsed;

        velocity.X *= dragFactor;
        velocity.Z *= dragFactor;

        return velocity;
    }

    private Vector3 ApplyGravity(GameTime gameTime, Vector3 currentVelocity)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 velocity = currentVelocity;

        // always apply gravity forces, and resolve collisions with tiles later
        velocity.Y = MathHelper.Clamp(currentVelocity.Y - GravityAcceleration * elapsed, -MaxFallVelocity, MaxFallVelocity);

        return velocity;
    }

    private void HandlePlayerCollisions()
    {
        foreach (Player opponent in GameMain.Match.Map.Players.Values.Where(p => p.PlayerId != _playerId))
        {
            ResolveCollision(BoundingBox, opponent.BoundingBox);
        }
    }

    private Pushback CheckHammerCollisions()
    {
        foreach (Hammer hammer in GameMain.Match.Map.Hammers.Values.Where(h => h.OwnerId != _playerId && h.State != HammerState.IS_HELD))
        {
            // detect collision
            if (BoundingBox.Intersects(hammer.BoundingBox))
            {
                OnHit(hammer);
                // Pushback distance could be modifiable based on charge
                return new Pushback(hammer.Direction, Pushback.PushbackDistance, Pushback.PushbackVelocity);
            }
        }
        return null;
    }

    /// <summary>
    /// Determines whether this player is falling by checking if there's a tile anywhere below it
    /// </summary>
    /// <returns>boolean value indicating whether this player is falling</returns>
    private bool IsFalling(Vector3 shift)
    {
        int x_low = (int)Math.Floor(((float)BoundingBox.Min.X + shift.X) / Tile.Width);
        int x_high = (int)Math.Ceiling((((float)BoundingBox.Max.X + shift.X) / Tile.Width)) - 1;
        int z_low = (int)Math.Floor((((float)BoundingBox.Min.Z + shift.Z) / Tile.Depth));
        int z_high = (int)Math.Ceiling(((float)BoundingBox.Max.Z + shift.Z) / Tile.Depth) - 1;

        for (int z = z_low; z <= z_high; z++)
        {
            for (int y = 0; y <= GameMain.Match.Map.Height; y++)
            {
                for (int x = x_low; x <= x_high; x++)
                {
                    // check if there's a tile below us
                    BoundingBox? neighbour = GameMain.Match.Map.TryGetTileBounds(x, y, z);
                    if (neighbour != null && ((BoundingBox)neighbour).Max.Y <= BoundingBox.Min.Y)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private float CalculateSmartDashDistance()
    {
        float distance = Dash.DashDistance;
        float step = MathF.Min(Size.X, Size.Z);
        while (distance > MinDashDistance)
        {
            Vector3 dashShift = Direction * distance;
            bool falling = IsFalling(dashShift);
            if (!falling)
            {
                return distance;
            }
            distance -= step;
        }
        return 0f;
    }

    private void OnHit(Hammer hammer)
    {
        hammer.Hit();
        GameMain.AudioManager.PlaySoundEffect(HammerHitSoundEffect);
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnFalling()
    {
        GameMain.AudioManager.PlaySoundEffect(PlayerFallingSoundEffect);
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnKilled()
    {
        Visible = false;
        Enabled = false;
        GamePad.SetVibration(_playerId, 0.0f, 0.0f, 0.0f, 0.0f);
    }

    public void PlayStepSoundEffect(GameTime gameTime)
    {
        if (_timeSinceLastStepMs > StepSoundEffectIntervalMs)
        {
            _stepIndex = (_stepIndex + 1) % NumStepSoundEffects;
            GameMain.AudioManager.PlaySoundEffect($"{StepSoundEffectPrefix}{_stepIndex}");
            _timeSinceLastStepMs = 0;
        }
        else
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            _timeSinceLastStepMs += elapsed;
        }
    }
}