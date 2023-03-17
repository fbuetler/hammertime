using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public class Player
{
    public int ID
    {
        get { return _id; }
    }
    private int _id;

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

    private Map _map;

    private Model _model;

    private Vector3 _pos;

    private Vector2 _movement;

    private Vector3 _velocity;

    public Boolean IsStandingOnTile
    {
        set { _isFalling = _isFalling || !value; }
        get { return !_isFalling; }
    }
    private Boolean _isFalling;

    public const int Height = 1;
    public const int Width = 1;
    public const int Depth = 1;

    // Constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveSpeed = 175f;
    private const float GroundDragFactor = 0.48f;
    private const float AirDragFactor = 0.58f;

    // Constants for controlling vertical movement
    private const float GravityAcceleration = 960f;
    private const float MaxFallSpeed = 340f;

    // Input configuration
    private const float MoveStickScale = 1.0f;

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
        _model = _map.Content.Load<Model>("RubiksCube");
    }

    public void Reset(Vector3 position)
    {
        _pos = position;
        _velocity = Vector3.Zero;
        _isFalling = false;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        GetInput(keyboardState, gamePadState);

        ApplyPhysics(gameTime);

        // clear input
        _movement = Vector2.Zero;
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
        if (_movement != Vector2.Zero)
        {
            _movement.Normalize();
        }

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

        if (!_isFalling)
        {
            _velocity *= GroundDragFactor;
        }
        else
        {
            _velocity *= AirDragFactor;
        }

        // apply velocity
        _pos += _velocity * elapsed;

        // if the player is now colliding with the map, separate them.
        HandleCollisions();

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
        if (_isFalling)
        {
            GamePad.SetVibration(_id, 0.5f, 0.5f, 0.5f, 0.5f);
            return velocityY;
        }

        return 0;
    }

    private void HandleCollisions()
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
                Vector3 depth = intersectionDepth(bounds, neighbour);
                if (depth == Vector3.Zero || depth.Y == 1)
                {
                    continue;
                }

                float absDepthX = Math.Abs(depth.X);
                float absDepthZ = Math.Abs(depth.Z);

                // resolve the collision along the shallow axis
                if (absDepthX < absDepthZ)
                {
                    if (absDepthX == 0 || absDepthX == 1)
                    {
                        continue;
                    }
                    _pos = new Vector3(
                        _pos.X + depth.X,
                        _pos.Y,
                        _pos.Z
                    );
                    bounds = BoundingBox;
                }
                else
                {
                    if (absDepthZ == 0 || absDepthZ == 1)
                    {
                        continue;
                    }
                    _pos = new Vector3(
                        _pos.X,
                        _pos.Y,
                        _pos.Z + depth.Z
                    );
                    bounds = BoundingBox;
                }
            }
        }
    }

    private Vector3 intersectionDepth(BoundingBox a, BoundingBox b)
    {
        // calculate half sizes
        float halfWidthA = a.Max.X - a.Min.X;
        float halfHeightA = a.Max.Y - a.Min.Y;
        float halfDepthA = a.Max.Z - a.Min.Z;
        float halfWidthB = b.Max.X - b.Min.X;
        float halfHeightB = b.Max.Y - b.Min.Y;
        float halfDepthB = b.Max.Z - b.Min.Z;

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

    public void Draw(GameTime gameTime)
    {
        // translate tiles
        Matrix translation = Matrix.CreateTranslation(_pos);
        Matrix translatedView = new Matrix();
        Matrix viewMatrix = _map.Camera.ViewMatrix;
        Matrix.Multiply(ref translation, ref viewMatrix, out translatedView);

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.AmbientLightColor = new Vector3(1f, 0, 0);
                effect.World = _map.Camera.WorldMatrix;
                effect.View = translatedView;
                effect.Projection = _map.Camera.ProjectionMatrix;
            }
            mesh.Draw();
        }

        _map.DebugDraw.Begin(_map.Camera.WorldMatrix, _map.Camera.ViewMatrix, _map.Camera.ProjectionMatrix);
        _map.DebugDraw.DrawWireBox(BoundingBox, Color.White);
        _map.DebugDraw.End();
    }
}