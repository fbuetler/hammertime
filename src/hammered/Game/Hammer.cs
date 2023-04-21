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

    private float _velocity;

    // charging distance
    private float _throwDistance;

    public override Vector3 MaxSize { get => _maxSize; }
    private static Vector3 _maxSize = new Vector3(0.5f, 0.5f, 0.5f);

    private HammerState _state;
    public override HammerState State { get => _state; }

    private Dictionary<HammerState, string> _objectModelPaths;
    public override Dictionary<HammerState, string> ObjectModelPaths { get => _objectModelPaths; }

    // constants for controlling throwing
    private const float ThrowAcceleration = 500f;
    private const float MaxThrowVelocity = 20f;
    public const float MaxThrowDistance = 10f;

    // constants for controlling pickup
    private const float PickupDistance = 1f;

    public Hammer(Game game, Vector3 position, int ownerId) : base(game, position + _maxSize / 2)
    {
        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.HAMMER_UPDATE_ORDER;
        Visible = false;
        DrawOrder = GameMain.HAMMER_DRAW_ORDER;

        _ownerId = ownerId;

        _state = HammerState.IS_HELD;

        _objectModelPaths = new Dictionary<HammerState, string>();
        _objectModelPaths[HammerState.IS_FLYING] = "Hammer/hammer";
        _objectModelPaths[HammerState.IS_RETURNING] = "Hammer/hammer";
        _objectModelPaths[HammerState.IS_HELD] = "Hammer/hammer";
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case HammerState.IS_HELD:
                Vector3 aimInput = ReadAimingInput();
                Direction = aimInput;
                break;
            case HammerState.IS_FLYING:
                bool collided = HandleTileCollisions();
                if (collided)
                {
                    Return();
                    break;
                }

                float travelledThrowDistance = (Center - _origin).Length();
                if (travelledThrowDistance > _throwDistance)
                {
                    // if max distance is reached, make it return
                    Return();
                    break;
                }

                // magic function: check wolfram alpha for the plot
                float travelledFraction = travelledThrowDistance / MaxThrowDistance;
                float y = 0.25f * MathF.Log(-travelledFraction + 1.05f) + 1f;
                _velocity = y * MaxThrowVelocity;

                Move(gameTime, Direction * _velocity);
                break;
            case HammerState.IS_RETURNING when (Center - GameMain.Match.Map.Players[_ownerId].Center).LengthSquared() < PickupDistance * PickupDistance || GameMain.Match.Map.Players[_ownerId].State == PlayerState.DEAD:
                // if hammer is close to the player or the player is dead, it is picked up
                PickUp();
                break;
            case HammerState.IS_RETURNING:
                HandleTileCollisions();
                FollowOwner();

                _velocity = ComputeVelocity(gameTime, _velocity, ThrowAcceleration);
                Move(gameTime, Direction * _velocity);
                break;
        }
    }

    private Vector3 ReadAimingInput()
    {
        Vector3 aimingDirection = Vector3.Zero;

        // get analog aim
        Vector2 a = Controls.Aim(_ownerId);
        aimingDirection.X = a.X;
        aimingDirection.Z = a.Y;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        aimingDirection.Z *= -1;

        // ignore small aiming input
        if (aimingDirection.LengthSquared() < 0.5f)
            aimingDirection = Vector3.Zero;

        // if any digital horizontal aiming input is found, override the analog aiming
        if (Controls.AimUp(_ownerId).Held())
        {
            aimingDirection.Z -= 1.0f;
        }
        else if (Controls.AimDown(_ownerId).Held())
        {
            aimingDirection.Z += 1.0f;
        }

        if (Controls.AimLeft(_ownerId).Held())
        {
            aimingDirection.X -= 1.0f;
        }
        else if (Controls.AimRight(_ownerId).Held())
        {
            aimingDirection.X += 1.0f;
        }

        return aimingDirection;
    }

    public void Throw(float throwDistance)
    {
        if (_state != HammerState.IS_HELD)
        {
            return;
        }

        // if there is no aiming input, use walking direction or default
        if (Direction == Vector3.Zero)
        {
            if (GameMain.Match.Map.Players[_ownerId].Direction != Vector3.Zero)
            {
                Direction = GameMain.Match.Map.Players[_ownerId].Direction;
            }
            else
            {
                Direction = new Vector3(1, 0, 0);
            }
        }

        // aiming is a unit vector (all in or nothing)
        float angle = MathF.Atan2(Direction.Z, Direction.X);
        Direction = new Vector3(
            MathF.Cos(angle),
            0,
            MathF.Sin(angle)
        );

        _velocity = MaxThrowVelocity;

        _state = HammerState.IS_FLYING;
        this.Visible = true;

        _origin = GameMain.Match.Map.Players[_ownerId].Center;
        Center = GameMain.Match.Map.Players[_ownerId].Center;

        _throwDistance = MathF.Min(throwDistance, MaxThrowDistance);
    }

    private void FollowOwner()
    {
        Vector3 dir = GameMain.Match.Map.Players[_ownerId].Center - Center;
        dir.Normalize();
        Direction = dir;
    }

    private void PickUp()
    {
        _state = HammerState.IS_HELD;
        _throwDistance = 0f;
        this.Visible = false;
        Direction = Vector3.Zero;
    }

    public void Hit()
    {
        Return();
    }

    public void Return()
    {
        if (_state == HammerState.IS_RETURNING)
        {
            return;
        }
        _state = HammerState.IS_RETURNING;
        _velocity = 0;
    }

    private float ComputeVelocity(GameTime gameTime, float currentVelocity, float acceleration)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float velocity = MathHelper.Clamp(currentVelocity + acceleration * elapsed, 0, MaxThrowVelocity);

        return velocity;
    }

}