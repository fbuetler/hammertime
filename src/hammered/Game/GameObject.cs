using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public record ScaledModel(Model model, Matrix modelScale);

public abstract class GameObject<GameObjectState> : DrawableGameComponent where GameObjectState : Enum
{
    private string _objectId;
    private Dictionary<GameObjectState, ScaledModel> _models;

    private GameMain _game;
    public GameMain GameMain { get => _game; }

    // TODO: (lmeinen) goal is to eventually make this private
    private Vector3 _pos;
    public Vector3 Position { get => _pos; set => _pos = value; }

    private Vector2 _dir = new Vector2(0, 0);
    public Vector2 Direction { get => _dir; set => _dir = value; }

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

    // TODO (fbuetler) Bounding box should rotate with hammer
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
        Position = position;
    }

    public void Move(GameTime gameTime, Vector3 velocity)
    {
        // TODO: (lmeinen) handle collisions
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += velocity * elapsed;
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
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Map.Camera.View;
        Matrix projection = GameMain.Map.Camera.Projection;

        float rotation = (float)Math.Atan(_dir.Y / _dir.X);
        Quaternion rotationQuaterion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotation);
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotationQuaterion);

        Matrix translation = Matrix.CreateTranslation(Position);

        // Matrix world = _models[State].modelScale * rotationMatrix * translation;
        Matrix world = _models[State].modelScale * translation;

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

}
