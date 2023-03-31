using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum PlayerState
{
    DEAD,
    FALLING,
    ALIVE,
    ALIVE_NO_HAMMER,
    PUSHBACK,
    PUSHBACK_NO_HAMMER,
    THROWING,
}

public class Player : GameObject<PlayerState>
{

    // sound
    private SoundEffect _hammerHitSound;
    private SoundEffect _killedSound;

    // player attributes
    public int PlayerId { get => _playerId; }
    private int _playerId;

    private Vector3 _velocity;

    public override Vector3 Size { get => new Vector3(1f, 1f, 1f); }

    private PlayerState _state;
    public override PlayerState State => _state;

    // push back
    private Vector3 _pushbackDir;
    private float _pushbackDistanceLeft;

    private Dictionary<PlayerState, string> _objectModelPaths;
    public override Dictionary<PlayerState, string> ObjectModelPaths => _objectModelPaths;

    // how far one gets pushed back by a hammer
    private const float PushbackDistance = 3f;
    private const float PushbackSpeed = 2000f;

    // if a player is below the kill plane, it disappears
    private const float KillPlaneLevel = -10f;

    // constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveSpeed = 175f;
    private const float GroundDragFactor = 0.48f;
    private const float AirDragFactor = 0.58f;

    // constants for controlling vertical movement
    private const float GravityAcceleration = 960f;
    private const float MaxFallSpeed = 340f;

    // input configuration
    private const float MoveStickScale = 1.0f;
    private const Buttons ThrowButton = Buttons.RightShoulder;

    public Player(Game game, Vector3 position, int playerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = true;

        _playerId = playerId;

        _state = PlayerState.ALIVE;

        _objectModelPaths = new Dictionary<PlayerState, string>();
        _objectModelPaths[PlayerState.ALIVE] = "Player/playerCube";
        _objectModelPaths[PlayerState.ALIVE_NO_HAMMER] = "Player/playerCube";
        _objectModelPaths[PlayerState.PUSHBACK] = "Player/playerCube";
        _objectModelPaths[PlayerState.PUSHBACK_NO_HAMMER] = "Player/playerCube";
        _objectModelPaths[PlayerState.THROWING] = "Player/playerCube";
        _objectModelPaths[PlayerState.FALLING] = "Player/playerCube";
        _objectModelPaths[PlayerState.DEAD] = "Player/playerCube";
        // TODO: (lmeinen) Add models for other states

        _velocity = Vector3.Zero;
    }

    protected override void LoadAudioContent()
    {
        _killedSound = GameMain.Map.Content.Load<SoundEffect>("Audio/Willhelm");
        _hammerHitSound = GameMain.Map.Content.Load<SoundEffect>("Audio/hammerBong");
    }

    public override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(_playerId);
        HandleInput(keyboardState, gamePadState);
        CheckHammerCollisions();

        ApplyPhysics(gameTime);

        if (_state == PlayerState.THROWING)
        {
            GameMain.Map.Hammers[_playerId].Throw();
            OnHammerThrow();
        }

        if (_state == PlayerState.FALLING && Position.Y < KillPlaneLevel)
        {
            OnKilled();
        }
    }

    private void HandleInput(KeyboardState keyboardState, GamePadState gamePadState)
    {
        Vector3 movement = Vector3.Zero;
        // get analog movement
        movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;
        movement.Z = gamePadState.ThumbSticks.Left.Y * MoveStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        movement.Z *= -1;

        // ignore small movements to prevent running in place
        if (movement.LengthSquared() < 0.5f)
            movement = Vector3.Zero;

        // if any digital horizontal movement input is found, override the analog movement
        if (gamePadState.IsButtonDown(Buttons.DPadUp) ||
            keyboardState.IsKeyDown(Keys.Up))
        {
            movement.Z -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadDown) ||
                 keyboardState.IsKeyDown(Keys.Down))
        {
            movement.Z += 1.0f;
        }

        if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
            keyboardState.IsKeyDown(Keys.Left))
        {
            movement.X -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                 keyboardState.IsKeyDown(Keys.Right))
        {
            movement.X += 1.0f;
        }

        // prevent the player from running faster than his top speed
        if (movement.LengthSquared() > 1)
        {
            movement.Normalize();
        }

        Direction = movement;

        // check if player is alive before throwing hammer
        if (_state == PlayerState.ALIVE && (keyboardState.IsKeyDown(Keys.Space) || gamePadState.IsButtonDown(ThrowButton)))
        {
            _state = PlayerState.THROWING;
        }
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector3 prevPos = Position;

        // base velocity is a combination of horizontal movement control and
        // acceleration downward due to gravity
        _velocity.X += Direction.X * MoveAcceleration * elapsed;
        _velocity.Z += Direction.Z * MoveAcceleration * elapsed;

        // always apply gravity forces, and resolve collisions with tiles later
        _velocity.Y = MathHelper.Clamp(_velocity.Y - GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

        if (_state != PlayerState.FALLING) // not falling
        {
            _velocity *= GroundDragFactor;
        }
        else
        {
            _velocity *= AirDragFactor;
        }

        HandlePushback(gameTime);

        // apply velocity
        Move(gameTime, _velocity);

        // if the player is now colliding with the map, separate them
        HandleTileCollisions();
        HandlePlayerCollisions();

        _pushbackDistanceLeft = Math.Max(0, _pushbackDistanceLeft - (prevPos - Position).Length());
        switch (_state)
        {
            case PlayerState.PUSHBACK when _pushbackDistanceLeft == 0:
                _state = PlayerState.ALIVE;
                break;
            case PlayerState.PUSHBACK_NO_HAMMER when _pushbackDistanceLeft == 0:
                _state = PlayerState.ALIVE_NO_HAMMER;
                break;
            default:
                // do nothing
                break;
        }

        // if the collision stopped us from moving, reset the velocity to zero
        if (Position.X == prevPos.X)
        {
            _velocity.X = 0;
        }

        if (Position.Y == prevPos.Y)
        {
            _velocity.Y = 0;
        }
        else if (_state != PlayerState.FALLING)
        {
            OnFalling();
        }

        if (Position.Z == prevPos.Z)
        {
            _velocity.Z = 0;
        }
    }

    private void HandlePushback(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if ((_state == PlayerState.PUSHBACK || _state == PlayerState.PUSHBACK_NO_HAMMER) && _pushbackDistanceLeft > 0)
        {
            _velocity = _pushbackDir * PushbackSpeed * elapsed;
        }
    }

    private void HandleTileCollisions()
    {
        BoundingBox bounds = BoundingBox;
        int x_low = (int)Math.Floor((float)bounds.Min.X / Tile.Width);
        int x_high = (int)Math.Ceiling(((float)bounds.Max.X / Tile.Width)) - 1;
        int z_low = (int)Math.Floor(((float)bounds.Min.Z / Tile.Depth));
        int z_high = (int)Math.Ceiling((float)bounds.Max.Z / Tile.Depth) - 1;

        // TODO (fbuetler) iterate over y as well to respect walls (only positive)
        for (int z = z_low; z <= z_high; z++)
        {
            for (int x = x_low; x <= x_high; x++)
            {
                // determine collision depth (with direction) and magnitude
                BoundingBox? neighbour = GameMain.Map.TryGetTileBounds(x, 0, z);
                if (neighbour != null)
                {
                    ResolveCollision(BoundingBox, (BoundingBox)neighbour);
                }
            }
        }
    }

    private void HandlePlayerCollisions()
    {
        foreach (Player opponent in GameMain.Map.Players.Values.Where(p => p.PlayerId != _playerId))
        {
            ResolveCollision(BoundingBox, opponent.BoundingBox);
        }
    }

    private void CheckHammerCollisions()
    {
        foreach (Hammer hammer in GameMain.Map.Hammers.Values.Where(h => h.OwnerId != _playerId && h.State != HammerState.IS_HELD))
        {
            // detect collision
            if (BoundingBox.Intersects(hammer.BoundingBox))
            {
                OnHit(hammer.Direction);
                hammer.Hit();
            }
        }
    }

    private void OnHammerThrow()
    {
        _state = PlayerState.ALIVE_NO_HAMMER;
        // TODO (fbuetler) update texture
    }

    public void OnHammerReturn()
    {
        if (_state == PlayerState.ALIVE_NO_HAMMER)
        {
            _state = PlayerState.ALIVE;
        }
        // TODO (fbuetler) update texture
    }

    public void OnHit(Vector3 pushbackDir)
    {
        switch (_state)
        {
            case PlayerState.ALIVE:
            case PlayerState.THROWING:
                _state = PlayerState.PUSHBACK;
                break;
            case PlayerState.ALIVE_NO_HAMMER:
                _state = PlayerState.PUSHBACK_NO_HAMMER;
                break;
            default:
                // ignore hammer hits in any other case
                return;
        }

        _hammerHitSound.Play();
        _pushbackDir = pushbackDir;
        _pushbackDistanceLeft = PushbackDistance;

        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnFalling()
    {
        _state = PlayerState.FALLING;
        _killedSound.Play();
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnKilled()
    {
        _state = PlayerState.DEAD;
        Visible = false;
        Enabled = false;
        GamePad.SetVibration(_playerId, 0.0f, 0.0f, 0.0f, 0.0f);
    }

}