using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum HammerState
{
    IS_FLYING,
    IS_RETURNING,
    IS_HELD
}

public class Hammer : GameObject<HammerState>
{

    private int _ownerId;
    public int OwnerId { get => _ownerId; }

    // hammer position
    private Vector3 _origin;
    public float Speed { get => _speed; }
    private float _speed;

    public override Vector3 Size { get => new Vector3(0.5f, 0.5f, 0.5f); }

    private HammerState _state;
    public override HammerState State { get => _state; }

    private Dictionary<HammerState, string> _objectModelPaths;
    public override Dictionary<HammerState, string> ObjectModelPaths { get => _objectModelPaths; }

    // constants for controlling throwing
    private const float ThrowSpeed = 20f;
    private const float MaxThrowDistance = 10f;
    private const float AimStickScale = 1.0f;
    private const float PickupDistance = 1f;

    // TODO (fbuetler) deacclerate when close to player on return/before hit

    public Hammer(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = false;

        _ownerId = ownerId;

        _state = HammerState.IS_HELD;

        _objectModelPaths = new Dictionary<HammerState, string>();
        _objectModelPaths[HammerState.IS_FLYING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_RETURNING] = "Hammer/hammerCube";
        _objectModelPaths[HammerState.IS_HELD] = "Hammer/hammerCube";

        _speed = ThrowSpeed;
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case HammerState.IS_HELD:
                HandleInput();
                break;
            case HammerState.IS_FLYING:
                Move(gameTime, Direction * ThrowSpeed);
                if ((Center - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
                {
                    // if max distance is reached, make it return
                    _state = HammerState.IS_RETURNING;
                }
                break;
            case HammerState.IS_RETURNING when (Center - GameMain.Map.Players[_ownerId].Center).LengthSquared() < PickupDistance * PickupDistance || GameMain.Map.Players[_ownerId].State == PlayerState.DEAD:
                // if hammer is close to the player or the player is dead, it is picked up
                PickUp();
                GameMain.Map.Players[_ownerId].OnHammerReturn();
                break;
            case HammerState.IS_RETURNING:
                FollowOwner();
                Move(gameTime, Direction * ThrowSpeed);
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

        // TODO (fbuetler) should we ignore small aiming inputs like the movement input

        // if any digital horizontal aiming input is found, override the analog aiming
        if (keyboardState.IsKeyDown(Keys.W))
        {
            aimingDirection.Z -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.S))
        {
            aimingDirection.Z += 1.0f;
        }

        if (keyboardState.IsKeyDown(Keys.A))
        {
            aimingDirection.X -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            aimingDirection.X += 1.0f;
        }

        Direction = aimingDirection;
    }

    public void Throw()
    {
        if (_state == HammerState.IS_HELD)
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

            // aiming is a unit vector
            float angle = MathF.Atan2(Direction.Z, Direction.X);
            Direction = new Vector3(
                MathF.Cos(angle),
                Direction.Y,
                MathF.Sin(angle)
            );

            _state = HammerState.IS_FLYING;
            this.Visible = true;

            _origin = GameMain.Map.Players[_ownerId].Center;
            Position = GameMain.Map.Players[_ownerId].Center - Size / 2;
        }
    }

    private void FollowOwner()
    {
        Vector3 dir = GameMain.Map.Players[_ownerId].Center - Center;
        dir.Normalize();
        Direction = dir;
    }

    private void PickUp()
    {
        _state = HammerState.IS_HELD;
        this.Visible = false;
        Direction = Vector3.Zero;
    }

    public void Hit()
    {
        _state = HammerState.IS_RETURNING;
    }

}