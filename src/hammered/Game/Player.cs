using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public class Player : GameObject
{
    private Map _map;

    // model
    private Model _model;
    private Matrix _modelScale;

    // sound
    private SoundEffect _hammerHitSound;
    private SoundEffect _killedSound;

    // attributes
    public int ID { get { return _id; } }
    private int _id;

    // movement
    public Vector3 Position { get { return _pos; } }
    private Vector3 _pos; // TODO (fbuetler) use center as positions instead of top left corner
    private Vector2 _movement;
    private Vector3 _velocity;

    // hammer
    public Hammer Hammer { get { return _hammer; } }
    private Hammer _hammer;
    private Vector2 _aiming;
    private bool _isThrowing;

    // charge
    private bool _wasChargePressed;
    private float _chargeDuration;
    private bool _isCharging;

    // push back
    private bool _isPushedback;
    private Vector2 _pushbackDir;
    private float _pushbackDistanceLeft;

    // a player is alive as long as it stands on the platform
    public bool IsAlive
    {
        get { return _isAlive; }
    }
    private bool _isAlive;

    // TODO (fbuetler) Bounding box should rotate with hammer
    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(
                new Vector3(_pos.X, _pos.Y, _pos.Z),
                new Vector3(_pos.X + Player.Width, _pos.Y + Player.Height, _pos.Z + Player.Depth)
            );
        }
    }

    // how far one gets pushed back by a hammer
    private const float PushbackDistance = 3f;
    private const float PushbackSpeed = 2000f;

    // if a player is below the kill plane, it disappears
    private const float KillPlaneLevel = -10f;

    // dimensions
    public const float Height = 1f;
    public const float Width = 1f;
    public const float Depth = 1f;

    // charge/throw
    // TODO (fbuetler) tweak unit
    private const float ChargeUnit = 0.005f;

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
    private const float AimStickScale = 1.0f;
    private const Buttons ThrowButton = Buttons.RightShoulder;

    public Player(Map map, int id, Vector3 position)
    {
        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _id = id;

        LoadContent();

        Reset(position);
    }

    public void LoadContent()
    {
        _model = _map.Content.Load<Model>("Player/playerCube");

        BoundingBox size = GetModelSize(_model);
        float xScale = Width / (size.Max.X - size.Min.X);
        float yScale = Height / (size.Max.Y - size.Min.Y);
        float zScale = Depth / (size.Max.Z - size.Min.Z);
        _modelScale = Matrix.CreateScale(xScale, yScale, zScale);

        _killedSound = _map.Content.Load<SoundEffect>("Audio/Willhelm");
        _hammerHitSound = _map.Content.Load<SoundEffect>("Audio/hammerBong");
    }

    public void Reset(Vector3 position)
    {
        _pos = position;
        _movement = Vector2.Zero;
        _velocity = Vector3.Zero;
        _hammer = new Hammer(_map, this);
        _aiming = Vector2.Zero;
        _isPushedback = false;
        _pushbackDistanceLeft = 0f;
        _isAlive = true;
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        GetMovementInput(keyboardState, gamePadState);
        GetAimingInput(keyboardState, gamePadState);

        ApplyPhysics(gameTime);

        DoThrowHammer();

        _hammer.Update(gameTime, keyboardState, gamePadState);

        // clear input
        _isThrowing = false;
    }

    private void GetMovementInput(KeyboardState keyboardState, GamePadState gamePadState)
    {
        // get analog movement
        _movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;
        _movement.Y = gamePadState.ThumbSticks.Left.Y * MoveStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        _movement.Y *= -1;

        // ignore small movements to prevent running in place
        if (_movement.LengthSquared() < 0.5f)
        {
            _movement = Vector2.Zero;
        }

        // if any digital horizontal movement input is found, override the analog movement
        if (gamePadState.IsButtonDown(Buttons.DPadUp) ||
            keyboardState.IsKeyDown(Keys.Up))
        {
            _movement.Y -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadDown) ||
                 keyboardState.IsKeyDown(Keys.Down))
        {
            _movement.Y += 1.0f;
        }

        if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
            keyboardState.IsKeyDown(Keys.Left))
        {
            _movement.X -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                 keyboardState.IsKeyDown(Keys.Right))
        {
            _movement.X += 1.0f;
        }


        // prevent the player from running faster than his top speed
        if (_movement.Length() > 1)
        {
            _movement.Normalize();
        }
    }

    private void GetAimingInput(KeyboardState keyboardState, GamePadState gamePadState)
    {
        // TODO (fbuetler) aiming should be possible into all directions

        // get analog aim
        _aiming.X = gamePadState.ThumbSticks.Right.X * AimStickScale;
        _aiming.Y = gamePadState.ThumbSticks.Right.Y * AimStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        _aiming.Y *= -1;

        // TODO (fbuetler) should we ignore small aiming inputs like the movement input

        // if any digital horizontal aiming input is found, override the analog aiming
        if (keyboardState.IsKeyDown(Keys.W))
        {
            _aiming.Y -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.S))
        {
            _aiming.Y += 1.0f;
        }

        if (keyboardState.IsKeyDown(Keys.A))
        {
            _aiming.X -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            _aiming.X += 1.0f;
        }

        // aiming is a unit vector
        _aiming.X = _aiming.X < 0 ? MathF.Floor(_aiming.X) : MathF.Ceiling(_aiming.X);
        _aiming.Y = _aiming.Y < 0 ? MathF.Floor(_aiming.Y) : MathF.Ceiling(_aiming.Y);
        if (_aiming.Length() > 1)
        {
            _aiming.Normalize();
        }

        // in case there is no input use the direction the player is facing
        // (allow playing with keyboard as well)
        if (_aiming.X == 0 && _aiming.Y == 0)
        {
            _aiming.X = _movement.X;
            _aiming.Y = _movement.Y;
        }
    }

    private void GetChargeInput(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        bool isChargePressed = (keyboardState.IsKeyDown(Keys.Space) || gamePadState.IsButtonDown(ThrowButton));
        if (!_wasChargePressed && isChargePressed)
        {
            _isCharging = true;
        }
        if (_isCharging)
        {
            _chargeDuration += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }
        // check if player is alive before throwing hammer
        if (_wasChargePressed && !isChargePressed && _isAlive)
        {
            _isThrowing = true;
        }
        _wasChargePressed = isChargePressed;
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector3 prevPos = _pos;

        // base velocity is a combination of horizontal movement control and
        // acceleration downward due to gravity
        _velocity.X += _movement.X * MoveAcceleration * elapsed;
        _velocity.Z += _movement.Y * MoveAcceleration * elapsed;
        _velocity.Y = MathHelper.Clamp(
            _velocity.Y - GravityAcceleration * elapsed,
            -MaxFallSpeed, MaxFallSpeed
        );

        HandleFall();

        if (_isAlive) // not falling
        {
            _velocity *= GroundDragFactor;
        }
        else
        {
            _velocity *= AirDragFactor;
        }

        HandleHammerCollisions();

        HandlePushback(gameTime);

        // apply velocity
        _pos += _velocity * elapsed;

        // if the player is now colliding with the map, separate them.
        HandleTileCollisions();
        HandlePlayerCollisions();

        // decrease push back distance by moved distance
        if (_isPushedback)
        {
            _pushbackDistanceLeft = Math.Max(0, _pushbackDistanceLeft - (prevPos - _pos).Length());
        }

        // if the collision stopped us from moving, reset the velocity and the pushbackDistanceLeft
        if (_pos.X == prevPos.X)
        {
            _velocity.X = 0;
            _pushbackDistanceLeft = 0f;
        }
        if (_pos.Y == prevPos.Y)
        {
            _velocity.Y = 0;
            _pushbackDistanceLeft = 0f;
        }

        if (_pos.Z == prevPos.Z)
        {
            _velocity.Z = 0;
            _pushbackDistanceLeft = 0f;
        }
    }

    private void HandleFall()
    {
        if (_isAlive)
        {
            _velocity.Y = 0;
        }
    }

    private void HandlePushback(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isPushedback && _pushbackDistanceLeft > 0)
        {
            _velocity.X = _pushbackDir.X * PushbackSpeed * elapsed;
            _velocity.Z = _pushbackDir.Y * PushbackSpeed * elapsed;
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
                TileCollision collision = _map.GetTileCollision(x, 0, z);
                if (collision == TileCollision.Passable)
                {
                    continue;
                }

                // determine collision depth (with direction) and magnitude
                BoundingBox neighbour = _map.GetTileBounds(x, 0, z);
                ResolveCollision(BoundingBox, neighbour);
            }
        }
    }

    private void HandlePlayerCollisions()
    {
        List<Player> opponents = _map.Players;
        foreach (Player opponent in opponents)
        {
            // dont do collision detection with itself
            if (opponent.ID == _id)
            {
                continue;
            }
            ResolveCollision(BoundingBox, opponent.BoundingBox);
        }
    }

    private void ResolveCollision(BoundingBox a, BoundingBox b)
    {
        Vector3 depth = IntersectionDepth(a, b);
        if (depth == Vector3.Zero)
        {
            return;
        }

        float absDepthX = Math.Abs(depth.X);
        float absDepthZ = Math.Abs(depth.Z);

        // resolve the collision along the shallow axis
        if (absDepthX < absDepthZ)
        {
            _pos = new Vector3(
                _pos.X + depth.X,
                _pos.Y,
                _pos.Z
            );
        }
        else
        {
            _pos = new Vector3(
                _pos.X,
                _pos.Y,
                _pos.Z + depth.Z
            );
        }
    }

    private void HandleHammerCollisions()
    {
        Hammer[] hammers = _map.GetHammers();
        foreach (Hammer hammer in hammers)
        {
            // dont do collision detection if the hammer is not flying or this player is its owner
            if (!hammer.IsFlying || hammer.OwnerID == _id)
            {
                continue;
            }

            // detect collision
            if (BoundingBox.Intersects(hammer.BoundingBox))
            {
                // TODO (fbuetler) can we remove this func and _hit?
                // only hit player, if it is not hit already by this hammer
                if (!hammer.IsPlayerHit(_id))
                {
                    OnHit(hammer.Dir);
                    hammer.OnHit(this._id, _pos);
                }
            }
        }
    }

    private Vector3 IntersectionDepth(BoundingBox a, BoundingBox b)
    {
        // calculate half sizes
        float halfWidthA = (a.Max.X - a.Min.X) * 0.5f;
        float halfHeightA = (a.Max.Y - a.Min.Y) * 0.5f;
        float halfDepthA = (a.Max.Z - a.Min.Z) * 0.5f;
        float halfWidthB = (b.Max.X - b.Min.X) * 0.5f;
        float halfHeightB = (b.Max.Y - b.Min.Y) * 0.5f;
        float halfDepthB = (b.Max.Z - b.Min.Z) * 0.5f;

        // calculate centers
        Vector3 centerA = new Vector3(a.Min.X + halfWidthA, a.Min.Y + halfHeightA, a.Min.Z + halfDepthA);
        Vector3 centerB = new Vector3(b.Min.X + halfWidthB, b.Min.Y + halfHeightB, b.Min.Z + halfDepthB);

        // Calculate current and minimum-non-intersecting distances between centers.
        float distanceX = centerA.X - centerB.X;
        float distanceY = centerA.Y - centerB.Y;
        float distanceZ = centerA.Z - centerB.Z;
        float minDistanceX = halfWidthA + halfWidthB;
        float minDistanceY = halfHeightA + halfHeightB;
        float minDistanceZ = halfDepthA + halfDepthB;

        // If we are not intersecting at all, return (0, 0).
        if (Math.Abs(distanceX) >= minDistanceX || Math.Abs(distanceY) >= minDistanceY || Math.Abs(distanceZ) >= minDistanceZ)
            return Vector3.Zero;

        // Calculate and return intersection depths.
        float depthX = distanceX > 0 ? minDistanceX - distanceX : -minDistanceX - distanceX;
        float depthY = distanceY > 0 ? minDistanceY - distanceY : -minDistanceY - distanceY;
        float depthZ = distanceZ > 0 ? minDistanceZ - distanceZ : -minDistanceZ - distanceZ;
        return new Vector3(depthX, depthY, depthZ);
    }

    private void DoThrowHammer()
    {
        // if player is killed, its hammer is deleted, once it starts returning
        if (!_isAlive && _hammer.IsReturning)
        {
            _hammer.Reset(this);
        }
        if (_isThrowing)
        {
            _hammer.Throw(_aiming, _chargeDuration * ChargeUnit);
            _chargeDuration = 0f;

            OnHammerThrow();
        }
        // TODO (fbuetler) update texture
    }

    private void OnHammerThrow()
    {
        // TODO (fbuetler) update texture
    }

    public void OnHammerReturn()
    {
        // TODO (fbuetler) update texture
    }

    public void OnHit(Vector2 pushbackDir)
    {
        _hammerHitSound.Play();
        _isPushedback = true;
        _pushbackDir = pushbackDir;
        _pushbackDistanceLeft = PushbackDistance;
    }

    public void OnKilled()
    {
        _isAlive = false;

        GamePad.SetVibration(_id, 0.2f, 0.2f, 0.2f, 0.2f);
        _killedSound.Play();
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        // dont draw player if its fell below the 
        if (!IsAlive && _pos.Y < KillPlaneLevel)
        {
            return;
        }

        Matrix translation = Matrix.CreateTranslation(_pos);

        // TODO (fbuetler) rotate player into walking direction

        Matrix world = _modelScale * translation;
        DrawModel(_model, world, view, projection);

        // TODO (fbuetler) calculate arrow positions/direction/length
        float aimingAngle = (float)Math.Acos((Vector2.Dot(Vector2.UnitX, _aiming)) / _aiming.LengthSquared());
        Matrix arrowRotation = Matrix.CreateRotationY(aimingAngle);
        Matrix arrowScale = Matrix.CreateScale(1f, 0.1f, 0.5f);
        world = arrowScale * arrowRotation * translation;
        DrawModel(_model, world, view, projection);

        _hammer.Draw(view, projection);

#if DEBUG
        _map.DebugDraw.Begin(Matrix.Identity, view, projection);
        _map.DebugDraw.DrawWireBox(BoundingBox, Color.White);
        _map.DebugDraw.End();
#endif
    }
}