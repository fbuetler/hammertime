using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public class Player : GameObject
{
    private Map _map;

    private Model _model;
    private Matrix _modelScale;

    public int ID { get { return _id; } }
    private int _id;

    // player state
    public Vector3 Position { get { return _pos; } }
    private Vector3 _pos; // TODO (fbuetler) use center as positions instead of top left corner
    private Vector2 _movement;
    private Vector3 _velocity;

    public Hammer Hammer { get { return _hammer; } }
    private Hammer _hammer;
    private Vector2 _aiming;
    private bool _isThrowing;

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

    // dimensions
    public const float Height = 1f;
    public const float Width = 1f;
    public const float Depth = 1f;

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
    }

    public void Reset(Vector3 position)
    {
        _pos = position;
        _velocity = Vector3.Zero;
        _isAlive = true;
        _hammer = new Hammer(_map, this);
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        GetInput(keyboardState, gamePadState);

        ApplyPhysics(gameTime);

        DoThrowHammer();

        _hammer.Update(gameTime, keyboardState, gamePadState);

        // clear input
        _movement = Vector2.Zero;
        _aiming = Vector2.Zero;
        _isThrowing = false;
    }

    private void GetInput(KeyboardState keyboardState, GamePadState gamePadState)
    {
        // get analog movement
        _movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;
        _movement.Y = gamePadState.ThumbSticks.Left.Y * MoveStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        _movement.Y *= -1;

        // ignore small movements to prevent running in place
        if (_movement.LengthSquared() < 0.5f)
            _movement = Vector2.Zero;

        // if any digital horizontal movement input is found, override the analog movement
        if (gamePadState.IsButtonDown(Buttons.DPadUp) ||
            keyboardState.IsKeyDown(Keys.Up) ||
            keyboardState.IsKeyDown(Keys.W))
        {
            _movement.Y -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadDown) ||
                 keyboardState.IsKeyDown(Keys.Down) ||
                 keyboardState.IsKeyDown(Keys.S))
        {
            _movement.Y += 1.0f;
        }

        if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
            keyboardState.IsKeyDown(Keys.Left) ||
            keyboardState.IsKeyDown(Keys.A))
        {
            _movement.X -= 1.0f;
        }
        else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                 keyboardState.IsKeyDown(Keys.Right) ||
                 keyboardState.IsKeyDown(Keys.D))
        {
            _movement.X += 1.0f;
        }

        // prevent the player from running faster than his top speed
        if (_movement.LengthSquared() > 1)
        {
            _movement.Normalize();
        }

        // get analog aim
        _aiming.X = gamePadState.ThumbSticks.Right.X * AimStickScale;
        _aiming.Y = gamePadState.ThumbSticks.Right.Y * AimStickScale;

        // flip y: on the thumbsticks, down is -1, but on the screen, down is bigger numbers
        _aiming.Y *= -1;

        // in case there is no input use the direction the player is facing
        // (allow playing with keyboard as well)
        if (_aiming.X == 0 && _aiming.Y == 0)
        {
            _aiming.X = _movement.X;
            _aiming.Y = _movement.Y;
        }

        // check if player is alive before throwing hammer
        _isThrowing = _isAlive && (keyboardState.IsKeyDown(Keys.Space) || gamePadState.IsButtonDown(ThrowButton));
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector3 prevPos = _pos;

        // base velocity is a combination of horizontal movement control and
        // acceleration downward due to gravity
        _velocity.X += _movement.X * MoveAcceleration * elapsed;
        _velocity.Z += _movement.Y * MoveAcceleration * elapsed;
        _velocity.Y = MathHelper.Clamp(_velocity.Y - GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

        _velocity.Y = WalkOffMap(gameTime, _velocity.Y);

        if (_isAlive) // not falling
        {
            _velocity *= GroundDragFactor;
        }
        else
        {
            _velocity *= AirDragFactor;
        }

        HandleHammerCollisions(gameTime);

        // apply velocity
        _pos += _velocity * elapsed;

        // if the player is now colliding with the map, separate them.
        HandleTileCollisions();
        HandlePlayerCollisions();

        // if the collision stopped us from moving, reset the velocity to zero
        if (_pos.X == prevPos.X)
            _velocity.X = 0;

        if (_pos.Y == prevPos.Y)
            _velocity.Y = 0;

        if (_pos.Z == prevPos.Z)
            _velocity.Z = 0;
    }

    private float WalkOffMap(GameTime gameTime, float velocityY)
    {
        if (_isAlive)
        {
            return 0;
        }

        return velocityY;
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

    private void HandleHammerCollisions(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

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
                // only hit player, if it is not hit already
                if (!hammer.IsPlayerHit(_id))
                {
                    hammer.HitPlayer(this._id, _pos.X, _pos.Z);
                    OnHit();
                }

                // TODO (fbuetler) can we remove this func and _hitX, _hitZ
                // TODO (fbuetler) give this constant a reasonable name
                if (hammer.CheckDist(_id, _pos.X, _pos.Z, 3f))
                {
                    _pos.X += hammer.Dir.X * elapsed * hammer.Speed;
                    _pos.Z += hammer.Dir.Y * elapsed * hammer.Speed;
                    _velocity.X = 0;
                    _velocity.Z = 0;
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
            _hammer.Throw(_aiming);

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

    public void OnHit()
    {
        // TODO (fbuetler) play sound
    }

    public void OnKilled()
    {
        _isAlive = false;

        GamePad.SetVibration(_id, 0.2f, 0.2f, 0.2f, 0.2f);

        // TODO (fbuetler) add fall sound
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        Matrix translation = Matrix.CreateTranslation(_pos);

        // TODO (fbuetler) rotate player into walking direction

        Matrix world = _modelScale * translation;
        DrawModel(_model, world, view, projection);

        _hammer.Draw(view, projection);

#if DEBUG
        _map.DebugDraw.Begin(Matrix.Identity, view, projection);
        _map.DebugDraw.DrawWireBox(BoundingBox, Color.White);
        _map.DebugDraw.End();
#endif
    }
}