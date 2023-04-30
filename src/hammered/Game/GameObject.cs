using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public record ScaledModel(Model model, Matrix modelScale, Vector3 size);

public abstract class GameObject<GameObjectState> : DrawableGameComponent where GameObjectState : Enum
{
    private string _objectId;

    public Vector3 Size { get => _size; }
    private Vector3 _size;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    public Vector3 Direction { get => _dir; set => _dir = value; }
    private Vector3 _dir = Vector3.Zero;

    public Vector3 Center { get => _center; set => _center = RoundVector(value, 2); }
    private Vector3 _center;


    // rotation center isn't necessarily equal to center for all models
    public virtual Vector3 RotCenter { get => Center; }

    public abstract Vector3 MaxSize
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

    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(Center - Size / 2, Center + Size / 2);
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

    public GameObject(Game game, Vector3 center) : this(game)
    {
        Center = center;
    }

    public float Move(GameTime gameTime, Vector3 velocity)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 displacement = velocity * elapsed;
        Vector3 tmpPos = Center;
        tmpPos += displacement;
        Center = tmpPos;
        return displacement.Length();
    }

    protected override void LoadContent()
    {
        // _models = new Dictionary<GameObjectState, ScaledModel>();
        foreach (GameObjectState state in Enum.GetValues(typeof(GameObjectState)))
        {
            string assetName = ObjectModelPaths[state];
            if (!GameMain.Match.Models.ContainsKey(assetName))
            {
                // load model
                Model model = GameMain.Match.Map.Content.Load<Model>(assetName);

                // compute scaling required to fit model to its BoundingBox
                BoundingBox size = GetModelSize(model);
                Console.WriteLine($"Loading model {assetName} with mesh size {size}");
                Console.WriteLine($"-- scaling it down to fit in a box of size {MaxSize}");
                float xScale = MaxSize.X / (size.Max.X - size.Min.X);
                float yScale = MaxSize.Y / (size.Max.Y - size.Min.Y);
                float zScale = MaxSize.Z / (size.Max.Z - size.Min.Z);

                // take the minimum to preserve model proportions
                float actualScalingFactor = Math.Min(xScale, Math.Min(yScale, zScale));
                Console.WriteLine($"-- computed scaling factor {actualScalingFactor}");
                _size = (size.Max - size.Min) * actualScalingFactor;
                Matrix modelScale = Matrix.CreateScale(actualScalingFactor);

                GameMain.Match.Models.Add(assetName, new ScaledModel(model, modelScale, Size));
            }
            else
            {
                _size = GameMain.Match.Models[assetName].size;
            }
        }

        LoadAudioContent();
    }

    protected virtual void LoadAudioContent() { }

    protected virtual Matrix ComputeScale()
    {
        return Matrix.Identity;
    }

    protected Matrix ComputeRotation()
    {
        float angle = MathF.Atan2(-Direction.Z, Direction.X);
        Matrix rotate = Matrix.CreateFromAxisAngle(Vector3.UnitY, angle);
        return rotate;
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Match.Map.Camera.View;
        Matrix projection = GameMain.Match.Map.Camera.Projection;

        Matrix rotate = ComputeRotation();

        Matrix translateIntoPosition = Matrix.CreateTranslation(RotCenter);

        Matrix world = GameMain.Match.Models[ObjectModelPaths[State]].modelScale * ComputeScale() * rotate * translateIntoPosition;

        DrawModel(GameMain.Match.Models[ObjectModelPaths[State]].model, world, view, projection);

#if DEBUG
        GameMain.Match.Map.DebugDraw.Begin(Matrix.Identity, view, projection);
        GameMain.Match.Map.DebugDraw.DrawWireBox(BoundingBox, GetDebugColor());
        GameMain.Match.Map.DebugDraw.End();
#endif
    }

    protected void DrawModel(Model model, Matrix world, Matrix view, Matrix projection)
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

    protected bool HandleTileCollisions()
    {
        BoundingBox bounds = BoundingBox;
        int x_low = (int)Math.Floor((float)bounds.Min.X / Tile.Width);
        int x_high = (int)Math.Ceiling(((float)bounds.Max.X / Tile.Width)) - 1;
        int y_low = (int)Math.Floor((float)bounds.Min.Y / Tile.Height); ;
        int y_high = (int)Math.Ceiling(((float)bounds.Max.Y / Tile.Height)) - 1;
        int z_low = (int)Math.Floor(((float)bounds.Min.Z / Tile.Depth));
        int z_high = (int)Math.Ceiling((float)bounds.Max.Z / Tile.Depth) - 1;

        bool collided = false;
        for (int z = z_low; z <= z_high; z++)
        {
            for (int y = y_low; y <= y_high; y++)
            {
                for (int x = x_low; x <= x_high; x++)
                {
                    // determine collision depth (with direction) and magnitude
                    BoundingBox? neighbour = GameMain.Match.Map.TryGetTileBounds(x, y, z);
                    if (neighbour != null)
                    {
                        ResolveCollision(BoundingBox, (BoundingBox)neighbour);
                        collided = true;
                    }
                }
            }
        }
        return collided;
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
            Center = new Vector3(
                Center.X + depth.X,
                Center.Y,
                Center.Z
            );
        }
        else if (absDepthY < absDepthX && absDepthY < absDepthZ)
        {
            Center = new Vector3(
                Center.X,
                Center.Y + depth.Y,
                Center.Z
            );
        }
        else
        {
            Center = new Vector3(
                Center.X,
                Center.Y,
                Center.Z + depth.Z
            );
        }
    }

    protected Vector3 IntersectionDepth(BoundingBox a, BoundingBox b)
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

    private Vector3 RoundVector(Vector3 vec, int numDigits)
    {
        return new Vector3(
            (float)Math.Round(vec.X, numDigits),
            (float)Math.Round(vec.Y, numDigits),
            (float)Math.Round(vec.Z, numDigits)
        );
    }

}
