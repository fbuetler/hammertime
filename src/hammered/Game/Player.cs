using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum PlayerState
{
    DEAD,
    FALLING,
    ALIVE,
    PUSHBACK,
    CHARGING,
    DASHING
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
    private static Vector3 _maxSize = new Vector3(1f, 1f, 1f);

    public override PlayerState State { get => _state; }
    private PlayerState _state;

    // charge
    public float ThrowDistance { get => _chargeDuration * ChargeUnit; }
    private float _chargeDuration;

    // note: this is null when we're not in a pushback state
    private Pushback _pushback;

    // note: this is null when we're not in a dashing state
    private Dash _dash;

    private Dictionary<PlayerState, string> _objectModelPaths;
    public override Dictionary<PlayerState, string> ObjectModelPaths => _objectModelPaths;

    // if a player is below the kill plane, it disappears
    private const float KillPlaneLevel = -10f;

    // charge/throw
    // TODO (fbuetler) tweak unit
    private const float ChargeUnit = 0.02f;

    // constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveVelocity = 175f;
    private const float GroundDragFactor = 0.48f;
    private const float AirDragFactor = 0.58f;

    // constants for controlling vertical movement
    private const float GravityAcceleration = 960f;
    private const float MaxFallVelocity = 340f;

    // sound effects
    private const string HammerHitSoundEffect = "hammerBong";
    private const string PlayerFallingSoundEffect = "falling";

    public Player(Game game, Vector3 position, int playerId) : base(game, position + _maxSize / 2)
    {
        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.PLAYER_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.PLAYER_DRAW_ORDER;

        _playerId = playerId;

        _state = PlayerState.ALIVE;

        _objectModelPaths = new Dictionary<PlayerState, string>();
        _objectModelPaths[PlayerState.ALIVE] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.PUSHBACK] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.FALLING] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.DEAD] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.DASHING] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.CHARGING] = "Player/playerNoHammer";
    }

    protected override void LoadAudioContent()
    {
        GameMain.AudioManager.LoadSoundEffect(PlayerFallingSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(HammerHitSoundEffect);
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 moveInput = ReadMovementInput();
        Vector3 prevCenter = Center;

        switch (State)
        {
            case PlayerState.ALIVE when Controls.Throw(_playerId).Held():
                _chargeDuration = 0;
                _state = PlayerState.CHARGING;
                break;
            case PlayerState.ALIVE when Controls.Dash(_playerId).Pressed():
                _dash = new Dash(Direction, Dash.DashDistance, Dash.DashVelocity);
                _state = PlayerState.DASHING;
                Visible = false;
                break;
            case PlayerState.ALIVE when moveInput != Vector3.Zero:
                Direction = moveInput;
                _velocity = ComputeVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                Move(gameTime, _velocity);
                break;
            case PlayerState.CHARGING when Controls.Throw(_playerId).Released():
                GameMain.Match.Map.Hammers[_playerId].Throw(ThrowDistance);
                _state = PlayerState.ALIVE;
                break;
            case PlayerState.CHARGING:
                // move
                if (moveInput != Vector3.Zero)
                {
                    Direction = moveInput;
                    _velocity = ComputeVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                    Move(gameTime, _velocity);
                }

                // charge
                _chargeDuration += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                break;
            case PlayerState.DASHING when _dash.Distance <= 0:
                _dash = null;
                _state = PlayerState.ALIVE;
                Visible = true;
                break;
            case PlayerState.DASHING:
                _velocity = ComputeVelocity(_velocity, _dash.Direction, _dash.Velocity, GroundDragFactor, gameTime);
                _dash.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.PUSHBACK when _pushback.Distance <= 0:
                _pushback = null;
                _state = PlayerState.ALIVE;
                break;
            case PlayerState.PUSHBACK:
                _velocity = ComputeVelocity(_velocity, _pushback.Direction, _pushback.Velocity, GroundDragFactor, gameTime);
                _pushback.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.FALLING when Center.Y < KillPlaneLevel:
                _state = PlayerState.DEAD;
                OnKilled();
                break;
            case PlayerState.FALLING:
                // TODO: (lmeinen) there's currently a bug where a player transitions into a FALLING state when they manage to cross a gap
                if (moveInput != Vector3.Zero)
                    Direction = moveInput;
                _velocity = ComputeVelocity(_velocity, Direction, MoveAcceleration, AirDragFactor, gameTime);
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

        if (IsFalling())
        {
            // Vertical velocity means we're falling :(
            if (State == PlayerState.ALIVE || State == PlayerState.PUSHBACK)
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
        if (movement.LengthSquared() < 0.5f)
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

    private Vector3 ComputeVelocity(Vector3 currentVelocity, Vector3 direction, float acceleration, float dragFactor, GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 velocity = currentVelocity + direction * acceleration * elapsed;

        // always apply gravity forces, and resolve collisions with tiles later
        velocity.Y = MathHelper.Clamp(currentVelocity.Y - GravityAcceleration * elapsed, -MaxFallVelocity, MaxFallVelocity);

        velocity *= dragFactor;

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
    private bool IsFalling()
    {
        int x_low = (int)Math.Floor((float)BoundingBox.Min.X / Tile.Width);
        int x_high = (int)Math.Ceiling(((float)BoundingBox.Max.X / Tile.Width)) - 1;
        int z_low = (int)Math.Floor(((float)BoundingBox.Min.Z / Tile.Depth));
        int z_high = (int)Math.Ceiling((float)BoundingBox.Max.Z / Tile.Depth) - 1;

        for (int z = z_low; z <= z_high; z++)
        {
            for (int y = 0; y <= GameMain.Match.Map.Height; y++)
            {
                for (int x = x_low; x <= x_high; x++)
                {
                    // check if there's a tile below us
                    BoundingBox? neighbour = GameMain.Match.Map.TryGetTileBounds(x, y, z);
                    if (neighbour != null && ((BoundingBox)neighbour).Max.Y < BoundingBox.Min.Y)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
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
        GameMain.Match.Map.AdjustSongSpeed();
    }

}