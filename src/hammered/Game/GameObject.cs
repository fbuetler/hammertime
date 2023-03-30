using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public record ScaledModel(Model model, Matrix modelScale);

public abstract class GameObject<GameObjectState> : DrawableGameComponent where GameObjectState : Enum
{
    private string _objectId;
    private Dictionary<GameObjectState, ScaledModel> _models;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    // TODO: (lmeinen) goal is to eventually make this private
    public Vector3 Position { get => _pos; set => _pos = value; }
    private Vector3 _pos;

    public Vector3 Direction { get => _dir; set => _dir = value; }
    private Vector3 _dir = Vector3.Zero;

    public Vector3 Center { get => Position + Size / 2; set => _pos = value - Size / 2; }

    public abstract Vector3 Size
    {
        get;
    }

    public abstract GameObjectState State
    {
        get;
    }

    public abstract Dictionary<GameObjectState, string> ObjectModelPaths
    {
        get;
    }

    // TODO (fbuetler) Bounding box should rotate with object
    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(
                new Vector3(Position.X, Position.Y, Position.Z),
                new Vector3(Position.X + Size.X, Position.Y + Size.Y, Position.Z + Size.Z)
            );
        }
    }

    protected virtual Color GetDebugColor()
    {
        return Color.Black;
    }

    public GameObject(Game game) : base(game)
    {
        _game = (GameMain)game;
        _objectId = Guid.NewGuid().ToString();
    }

    public GameObject(Game game, Vector3 position) : this(game)
    {
        _pos = position;
    }

    public void Move(GameTime gameTime, Vector3 velocity)
    {
        // TODO: (lmeinen) handle collisions
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pos += velocity * elapsed;
    }

    protected override void LoadContent()
    {
        _models = new Dictionary<GameObjectState, ScaledModel>();
        foreach (GameObjectState state in Enum.GetValues(typeof(GameObjectState)))
        {
            // load model
            Model model = GameMain.Map.Content.Load<Model>(ObjectModelPaths[state]);

            // compute scaling required to fit model to its BoundingBox
            BoundingBox size = GetModelSize(model);
            float xScale = Size.X / (size.Max.X - size.Min.X);
            float yScale = Size.Y / (size.Max.Y - size.Min.Y);
            float zScale = Size.Z / (size.Max.Z - size.Min.Z);
            Matrix modelScale = Matrix.CreateScale(xScale, yScale, zScale);

            _models[state] = new ScaledModel(model, modelScale);
        }

        LoadAudioContent();
    }

    protected virtual void LoadAudioContent() { }

    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Map.Camera.View;
        Matrix projection = GameMain.Map.Camera.Projection;

        // as the model is rotate we have to
        // * move it into the origin
        // * rotate
        // * move it into it designated positions and also compensate for the move into the origin
        Matrix translateIntoOrigin = Matrix.CreateTranslation(-Size / 2);

        float angle = MathF.Atan2(_dir.Y, _dir.X);
        Matrix rotate = Matrix.CreateFromAxisAngle(Vector3.UnitY, angle);

        Matrix translateIntoPosition = Matrix.CreateTranslation(Center);

        Matrix world = _models[State].modelScale * translateIntoOrigin * rotate * translateIntoPosition;

        // change tile model based on current object state
        DrawModel(_models[State].model, world, view, projection);

#if DEBUG
        GameMain.Map.DebugDraw.Begin(Matrix.Identity, view, projection);
        GameMain.Map.DebugDraw.DrawWireBox(BoundingBox, GetDebugColor());
        GameMain.Map.DebugDraw.End();
#endif
    }

    private void DrawModel(Model model, Matrix world, Matrix view, Matrix projection)
    {
        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.EnableDefaultLighting();
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;
            }

            mesh.Draw();
        }
    }

    private BoundingBox GetModelSize(Model model)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (ModelMeshPart meshPart in mesh.MeshParts)
            {
                int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                int vertexBufferSize = meshPart.NumVertices * vertexStride;

                int vertexDataSize = vertexBufferSize / sizeof(float);
                float[] vertexData = new float[vertexDataSize];
                meshPart.VertexBuffer.GetData<float>(vertexData);

                for (int i = 0; i < vertexDataSize; i += vertexStride / sizeof(float))
                {
                    Vector3 vertex = new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
            }
        }

        return new BoundingBox(min, max);
    }

    protected void ResolveCollision(BoundingBox a, BoundingBox b)
    {
        Vector3 depth = IntersectionDepth(a, b);
        if (depth == Vector3.Zero)
        {
            return;
        }

        float absDepthX = Math.Abs(depth.X);
        float absDepthY = Math.Abs(depth.Y);
        float absDepthZ = Math.Abs(depth.Z);

        // resolve the collision along the shallow axis
        if (absDepthX < absDepthY && absDepthX < absDepthZ)
        {
            Position = new Vector3(
                Position.X + depth.X,
                Position.Y,
                Position.Z
            );
        }
        else if (absDepthY < absDepthX && absDepthY < absDepthZ)
        {
            Position = new Vector3(
                Position.X,
                Position.Y + depth.Y,
                Position.Z
            );
        }
        else
        {
            Position = new Vector3(
                Position.X,
                Position.Y,
                Position.Z + depth.Z
            );
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

}
