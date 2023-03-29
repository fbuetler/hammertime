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
                if ((Position - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
                {
                    // if max distance is reached, make it return
                    _state = HammerState.IS_RETURNING;
                    _hitPlayers.Clear();
                }
                break;
            case HammerState.IS_RETURNING when (Position - GameMain.Map.Players[_ownerId].Position).LengthSquared() < 1f || GameMain.Map.Players[_ownerId].State == PlayerState.DEAD:
                // hammer is close to the player or the player is dead, it is returned
                _state = HammerState.IS_NOT_FLYING;
                this.Visible = false;
                Direction = Vector3.Zero;
                _hitPlayers.Clear();
                GameMain.Map.Players[_ownerId].OnHammerReturn();
                break;
            case HammerState.IS_RETURNING:
                Vector3 dir = GameMain.Map.Players[_ownerId].Position - Position;
                dir.Normalize(); // can't work on Direction directly, as Vector3 is a struct, not an object
                Direction = dir;
                Move(gameTime, Direction * ThrowSpeed);
                break;
            case HammerState.IS_NOT_FLYING:
                Position = GameMain.Map.Players[_ownerId].Position + new Vector3(0.25f, 0.25f, 0.25f);
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
        // TODO: (lmeinen) If direction is 0 then we're just throwing downwards - which should be fine really?
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

    public bool HitPlayer(int id, Vector3 pos)
    {
        var isHit = _hitPlayers.Add(id);
        if (isHit)
            _hitPos[id] = pos;
        return isHit;
    }

    public bool CheckDistFromHit(int id, Vector3 pos, float maxdist)
    {
        return (float)Math.Sqrt(((_hitPos[id].X - pos.X) * (_hitPos[id].X - pos.X)) + ((_hitPos[id].Y - pos.Y) * (_hitPos[id].Y - pos.Y))) <= maxdist;
    }

}