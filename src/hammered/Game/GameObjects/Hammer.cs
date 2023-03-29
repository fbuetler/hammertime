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

    public int OwnerID { get { return _owner.PlayerId; } }
    private Player _owner;

    // hammer position
    private Vector3 _origin;
    public float Speed { get { return _speed; } }
    private float _speed;

    // hammer hit
    private bool[] _playerHit = new bool[] { false, false, false, false };
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

    // TODO (fbuetler) deacclerate when close to player on return/before hit

    public Hammer(Game game, Vector3 position, Player owner) : base(game, position)
    {
        this.Enabled = true;
        this.Visible = false;

        _owner = owner;
        _state = HammerState.IS_NOT_FLYING;
        _objectModelPaths = new Dictionary<HammerState, string>();
        _objectModelPaths[HammerState.IS_FLYING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_RETURNING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_NOT_FLYING] = "Hammer/hammerCube";
        _speed = ThrowSpeed;
        _playerHit = new bool[] { false, false, false, false };
    }

    // TODO: (lmeinen) remove
    public void Reset(Player owner)
    {
        _owner = owner;
        // TODO (fred) replace the speed with some actual speed. not just a fixed number
        _speed = ThrowSpeed;
        _playerHit = new bool[] { false, false, false, false };
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 pos = Position;
        switch (_state)
        {
            case HammerState.IS_FLYING:
                Move(gameTime, Direction * ThrowSpeed);
                if ((Position - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
                {
                    // if max distance is reached, make it return
                    _state = HammerState.IS_RETURNING;
                    _playerHit = new bool[] { false, false, false, false };
                }
                break;
            case HammerState.IS_RETURNING when (Position - _owner.Position).LengthSquared() < 1f || _owner.State == PlayerState.DEAD:
                // hammer is close to the player or the player is dead, it is returned
                _state = HammerState.IS_NOT_FLYING;
                this.Visible = false;
                Direction = Vector3.Zero;
                _playerHit = new bool[] { false, false, false, false };
                _owner.OnHammerReturn();
                break;
            case HammerState.IS_RETURNING:
                Vector3 dir = _owner.Position - Position;
                dir.Normalize(); // can't work on Direction directly, as Vector3 is a struct, not an object
                Direction = dir;
                Move(gameTime, Direction * ThrowSpeed);
                break;
            case HammerState.IS_NOT_FLYING:
                Position = _owner.Position;
                HandleInput();
                break;
        }
    }

    private void HandleInput()
    {
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(_owner.PlayerId);

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
        // TODO: (lmeinen) If direction is 0 then we're just throwing downwards - which should be fine really?
        if (_state == HammerState.IS_NOT_FLYING)
        {
            if (Direction == Vector3.Zero)
            {
                if (_owner.Direction != Vector3.Zero)
                {
                    Direction = _owner.Direction;
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

    public void HitPlayer(int id, Vector3 pos)
    {
        _playerHit[id] = true;
        _hitPos[id] = pos;
    }

    public bool IsPlayerHit(int i)
    {
        return _playerHit[i];
    }

    public bool CheckDistFromHit(int id, Vector3 pos, float maxdist)
    {
        return (float)Math.Sqrt(((_hitPos[id].X - pos.X) * (_hitPos[id].X - pos.X)) + ((_hitPos[id].Y - pos.Y) * (_hitPos[id].Y - pos.Y))) <= maxdist;
    }

}