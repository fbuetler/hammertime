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
    CHARGING,
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
    private SoundEffect _fallingSound;

    // player attributes
    public int PlayerId { get => _playerId; }
    private int _playerId;

    private Vector3 _velocity;

    // TODO (fbuetler) if a player is smaller than a tile it is immediately falling upon start
    public override Vector3 MaxSize { get => _maxSize;  set => _maxSize = value;}
    private static Vector3 _maxSize = new Vector3(1f, 1f, 1f);
    //public override Vector3 Size { get => _sizeVec; set => _sizeVec = value;}
    //private Vector3 _sizeVec = new Vector3(1f, 1f, 1f);

    private PlayerState _state;
    public override PlayerState State => _state;

    // charge
    public float _chargeDuration;

    // note: this is null when we're not in a pushback state
    private Pushback _pushback;

    private Dictionary<PlayerState, string> _objectModelPaths;
    public override Dictionary<PlayerState, string> ObjectModelPaths => _objectModelPaths;


    // how far one gets pushed back by a hammer
    private const float PushbackDistance = 3f;
    private const float PushbackVelocity = 2000f;

    // if a player is below the kill plane, it disappears
    private const float KillPlaneLevel = -10f;

    // charge/throw
    // TODO (fbuetler) tweak unit
    private const float ChargeUnit = 0.05f;

    // constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveVelocity = 175f;
    private const float GroundDragFactor = 0.48f;
    private const float AirDragFactor = 0.58f;

    // constants for controlling vertical movement
    private const float GravityAcceleration = 960f;
    private const float MaxFallVelocity = 340f;

    // input configuration
    private const float MoveStickScale = 1.0f;
    //private const Buttons ThrowButton = Buttons.RightShoulder;
    private const Buttons ThrowButton = Buttons.RightStick;
    //private GamePadState PreviousPadState;
    private int ChargeCounter = 0;
    private int GameStateCounter = 0;

    public Player(Game game, Vector3 position, int playerId) : base(game, position + _maxSize / 2)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = true;

        _playerId = playerId;

        _state = PlayerState.ALIVE;

        _objectModelPaths = new Dictionary<PlayerState, string>();
        _objectModelPaths[PlayerState.ALIVE] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.ALIVE_NO_HAMMER] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.PUSHBACK] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.PUSHBACK_NO_HAMMER] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.THROWING] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.FALLING] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.FALLING_NO_HAMMER] = "Player/playerNoHammer";
        _objectModelPaths[PlayerState.DEAD] = "Player/playerNoHammer";
        // TODO: (lmeinen) Add models for other states

        _velocity = Vector3.Zero;
        _chargeDuration = 0f;
    }

    protected override void LoadAudioContent()
    {
        _fallingSound = GameMain.Map.Content.Load<SoundEffect>("Audio/falling");
        _hammerHitSound = GameMain.Map.Content.Load<SoundEffect>("Audio/hammerBong");
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: (lmeinen) Both Hammer and Player now have Hammer.is_held type states - only one needs to store that info (buzzword: concurrent state machines)
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(_playerId);
        Vector3 moveInput = ReadMovementInput(keyboardState, gamePadState);
        Vector3 prevCenter = Center;

        GameStateCounter++;
        switch (State)
        {
            case PlayerState.ALIVE when IsTryingToThrow(keyboardState, gamePadState):
                _chargeDuration = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                _state = PlayerState.CHARGING;
               
                break;
            case PlayerState.ALIVE when moveInput != Vector3.Zero:
            case PlayerState.ALIVE_NO_HAMMER when moveInput != Vector3.Zero:
                Direction = moveInput;
                _velocity = ComputeVelocity(_velocity, Direction, MoveAcceleration, GroundDragFactor, gameTime);
                Move(gameTime, _velocity);
                break;
            case PlayerState.CHARGING when !IsTryingToThrow(keyboardState, gamePadState):
                _state = PlayerState.THROWING;
                ChargeCounter = 0;
                break;
            case PlayerState.CHARGING:
                _chargeDuration += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                ChargeCounter++;
                //GameMain.Map.Arrows[_playerId].Charge(_chargeDuration);
                break;
            case PlayerState.THROWING:
                GameMain.Map.Hammers[_playerId].Throw(_chargeDuration * ChargeUnit);
                _state = PlayerState.ALIVE_NO_HAMMER;
                //GameMain.Map.Arrows[_playerId].Charge(0f);
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
                _velocity = ComputeVelocity(_velocity, _pushback.Direction, PushbackVelocity, GroundDragFactor, gameTime);
                _pushback.Distance -= Move(gameTime, _velocity);
                break;
            case PlayerState.FALLING when Center.Y < KillPlaneLevel:
            case PlayerState.FALLING_NO_HAMMER when Center.Y < KillPlaneLevel:
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

        HandlePlayerCollisions();
        HandleTileCollisions();

        Pushback pushback = CheckHammerCollisions();
        if (pushback != null && _pushback == null)
        {
            _pushback = (Pushback)pushback;
            if (State == PlayerState.FALLING || State == PlayerState.ALIVE)
                _state = PlayerState.PUSHBACK;
            else if (State == PlayerState.FALLING_NO_HAMMER || State == PlayerState.ALIVE_NO_HAMMER)
                _state = PlayerState.PUSHBACK_NO_HAMMER;
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

    public float Charge()
    {
       return _chargeDuration;
    }

    private bool IsTryingToThrow(KeyboardState keyboardState, GamePadState gamePadState) => keyboardState.IsKeyDown(Keys.Space) || gamePadState.IsButtonDown(ThrowButton);

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
                OnHit(hammer);
                // Pushback distance could be modifiable based on charge
                return new Pushback(hammer.Direction, PushbackDistance);
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
            for (int y = 0; y <= GameMain.Map.Height; y++)
            {
                for (int x = x_low; x <= x_high; x++)
                {
                    // check if there's a tile below us
                    BoundingBox? neighbour = GameMain.Map.TryGetTileBounds(x, y, z);
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
        _hammerHitSound.Play();
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
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
        _fallingSound.Play();
        GamePad.SetVibration(_playerId, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    public void OnKilled()
    {
        Visible = false;
        Enabled = false;
        GamePad.SetVibration(_playerId, 0.0f, 0.0f, 0.0f, 0.0f);
    }
}