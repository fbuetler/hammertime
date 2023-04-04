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
    FALLING_NO_HAMMER,
    ALIVE,
    ALIVE_NO_HAMMER,
    PUSHBACK,
    PUSHBACK_NO_HAMMER,
    THROWING,
}

public class Pushback
{
    public Pushback(Vector3 direction, float distance)
    {
        Direction = direction;
        Distance = distance;
    }
    public Vector3 Direction { get; set; }
    public float Distance { get; set; }
};

public class Player : GameObject<PlayerState>
{

    // sound
    private SoundEffect _hammerHitSound;
    private SoundEffect _killedSound;

    // player attributes
    public int PlayerId { get => _playerId; }
    private int _playerId;

    private Vector3 _velocity;

    // TODO (fbuetler) if a player is smaller than a tile it is immediately falling upon start
    public override Vector3 Size { get => new Vector3(1f, 1f, 1f); }

    private PlayerState _state;
    public override PlayerState State => _state;

    // note: this is null when we're not in a pushback state
    private Pushback _pushback;

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
        _objectModelPaths[PlayerState.FALLING_NO_HAMMER] = "Player/playerCube";
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
        // TODO: (lmeinen) Both Hammer and Player now have Hammer.is_held type states - only one needs to store that info (buzzword: concurrent state machines)
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(_playerId);
        Vector3 moveInput = ReadMovementInput(keyboardState, gamePadState);
        Vector3 prevPos = Position;

        switch (State)
        {
            case PlayerState.THROWING:
                GameMain.Map.Hammers[_playerId].Throw();
                _state = PlayerState.ALIVE_NO_HAMMER;
                break;
            case PlayerState.ALIVE when IsTryingToThrow(keyboardState, gamePadState):
                _state = PlayerState.THROWING;
                break;
            case PlayerState.ALIVE when moveInput != Vector3.Zero:
            case PlayerState.ALIVE_NO_HAMMER when moveInput != Vector3.Zero:
                Direction = moveInput;
                _velocity = ComputeVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                Move(gameTime, _velocity);
                break;
            case PlayerState.PUSHBACK when _pushback.Distance <= 0:
                _pushback = null;
                _state = PlayerState.ALIVE;
                break;
            case PlayerState.PUSHBACK_NO_HAMMER when _pushback.Distance <= 0:
                _pushback = null;
                _state = PlayerState.ALIVE_NO_HAMMER;
                break;
            case PlayerState.PUSHBACK:
            case PlayerState.PUSHBACK_NO_HAMMER:
                _velocity = ComputeVelocity(_velocity, _pushback.Direction, PushbackSpeed, GroundDragFactor, gameTime);
                _pushback.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.FALLING when Position.Y < KillPlaneLevel:
            case PlayerState.FALLING_NO_HAMMER when Position.Y < KillPlaneLevel:
                _state = PlayerState.DEAD;
                OnKilled();
                break;
            case PlayerState.FALLING:
            case PlayerState.FALLING_NO_HAMMER:
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

        Pushback pushback = CheckHammerCollisions();
        if (pushback != null)
        {
            _pushback = (Pushback)pushback;
            if (State == PlayerState.FALLING || State == PlayerState.ALIVE)
                _state = PlayerState.PUSHBACK;
            else if (State == PlayerState.FALLING_NO_HAMMER || State == PlayerState.ALIVE_NO_HAMMER)
                _state = PlayerState.PUSHBACK_NO_HAMMER;
        }

        HandlePlayerCollisions();
        HandleTileCollisions();

        // if collision prevented us from moving, reset velocity
        if (prevPos.X == Position.X)
            _velocity.X = 0;
        if (prevPos.Y == Position.Y)
            _velocity.Y = 0;
        if (prevPos.Z == Position.Z)
            _velocity.Z = 0;

        if (_velocity.Y != 0)
        {
            // Vertical velocity means we're falling :(
            if (_state == PlayerState.ALIVE || _state == PlayerState.PUSHBACK)
            {
                _state = PlayerState.FALLING;
                OnFalling();
            }
            else if (State == PlayerState.ALIVE_NO_HAMMER || State == PlayerState.PUSHBACK_NO_HAMMER)
            {
                _state = PlayerState.FALLING_NO_HAMMER;
                OnFalling();
            }
        }
    }

    private Vector3 ReadMovementInput(KeyboardState keyboardState, GamePadState gamePadState)
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

        return movement;
    }

    private bool IsTryingToThrow(KeyboardState keyboardState, GamePadState gamePadState) => keyboardState.IsKeyDown(Keys.Space) || gamePadState.IsButtonDown(ThrowButton);

    private Vector3 ComputeVelocity(Vector3 currentVelocity, Vector3 direction, float acceleration, float dragFactor, GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 velocity = currentVelocity + direction * acceleration * elapsed;

        // always apply gravity forces, and resolve collisions with tiles later
        velocity.Y = MathHelper.Clamp(currentVelocity.Y - GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

        velocity *= dragFactor;

        return velocity;
    }

    private void HandlePlayerCollisions()
    {
        foreach (Player opponent in GameMain.Map.Players.Values.Where(p => p.PlayerId != _playerId))
        {
            ResolveCollision(BoundingBox, opponent.BoundingBox);
        }
    }

    private Pushback CheckHammerCollisions()
    {
        foreach (Hammer hammer in GameMain.Map.Hammers.Values.Where(h => h.OwnerId != _playerId && h.State != HammerState.IS_HELD))
        {
            // detect collision
            if (BoundingBox.Intersects(hammer.BoundingBox))
            {
                hammer.Hit();
                _hammerHitSound.Play();
                GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);

                // Pushback distance could be modifiable based on charge
                return new Pushback(hammer.Direction, PushbackDistance);
            }
        }
        return null;
    }

    public void OnHammerReturn()
    {
        if (State == PlayerState.ALIVE_NO_HAMMER)
        {
            _state = PlayerState.ALIVE;
        }
        // TODO (fbuetler) update texture
    }

    public void OnFalling()
    {
        _killedSound.Play();
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnKilled()
    {
        Visible = false;
        Enabled = false;
        GamePad.SetVibration(_playerId, 0.0f, 0.0f, 0.0f, 0.0f);
    }

}