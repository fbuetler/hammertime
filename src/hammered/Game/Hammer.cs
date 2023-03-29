using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum HammerState
{
    IS_FLYING,
    IS_RETURNING,
    IS_NOT_FLYING
}

public class Hammer : GameObject<HammerState>
{

    private int _ownerId;
    public int OwnerID { get => _ownerId; }

    // hammer position
    private Vector3 _origin;
    public float Speed { get { return _speed; } }
    private float _speed;

    // hammer hit
    private HashSet<int> _hitPlayers;
    private Vector3[] _hitPos = new Vector3[] {
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0f, 0f)
    };

    public override Vector3 Size => new Vector3(0.5f, 0.5f, 0.5f);

    private HammerState _state;
    public override HammerState State => _state;

    private Dictionary<HammerState, string> _objectModelPaths;
    public override Dictionary<HammerState, string> ObjectModelPaths => _objectModelPaths;

    // constants for controlling throwing
    private const float ThrowSpeed = 20f;
    private const float MaxThrowDistance = 10f;
    private const float AimStickScale = 1.0f;
    private const float PickupDistance = 1f;

    // TODO (fbuetler) deacclerate when close to player on return/before hit

    public Hammer(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        this.Enabled = true;
        this.Visible = false;

        _ownerId = ownerId;
        _state = HammerState.IS_NOT_FLYING;
        _objectModelPaths = new Dictionary<HammerState, string>();
        _objectModelPaths[HammerState.IS_FLYING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_RETURNING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_NOT_FLYING] = "Hammer/hammerCube";
        _speed = ThrowSpeed;
        _hitPlayers = new HashSet<int>();
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case HammerState.IS_FLYING:
                Move(gameTime, Direction * ThrowSpeed);
                if ((Center - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
                {
                    // if max distance is reached, make it return
                    _state = HammerState.IS_RETURNING;
                }
                break;
            case HammerState.IS_RETURNING when (Center - GameMain.Map.Players[_ownerId].Center).LengthSquared() < PickupDistance || GameMain.Map.Players[_ownerId].State == PlayerState.DEAD:
                // hammer is close to the player or the player is dead, it is returned
                _state = HammerState.IS_NOT_FLYING;
                this.Visible = false;
                Direction = Vector3.Zero;
                _hitPlayers.Clear();
                GameMain.Map.Players[_ownerId].OnHammerReturn();
                break;
            case HammerState.IS_RETURNING:
                Vector3 dir = GameMain.Map.Players[_ownerId].Center - Center;
                dir.Normalize(); // can't work on Direction directly, as Vector3 is a struct, not an object
                Direction = dir;
                Move(gameTime, Direction * ThrowSpeed);
                break;
            case HammerState.IS_NOT_FLYING:
                Center = GameMain.Map.Players[_ownerId].Center;
                HandleInput();
                break;
        }
    }

    private void HandleInput()
    {
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(_ownerId);

        // get analog aim
        Vector3 aimingDirection = Vector3.Zero;
        aimingDirection.X = gamePadState.ThumbSticks.Right.X * AimStickScale;
        aimingDirection.Z = gamePadState.ThumbSticks.Right.Y * AimStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        aimingDirection.Z *= -1;

        Direction = aimingDirection;
    }

    public void Throw()
    {
        if (_state == HammerState.IS_NOT_FLYING)
        {
            if (Direction == Vector3.Zero)
            {
                if (GameMain.Map.Players[_ownerId].Direction != Vector3.Zero)
                {
                    Direction = GameMain.Map.Players[_ownerId].Direction;
                }
                else
                {
                    Direction = new Vector3(0, 0, 1);
                }
            }
            Direction.Normalize();

            _state = HammerState.IS_FLYING;
            this.Visible = true;
            _origin = Position;
        }
    }

    public void Hit()
    {
        _state = HammerState.IS_RETURNING;
    }

}