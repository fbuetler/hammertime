using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public abstract class GameObject
{
    public abstract void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState);

    public abstract void Draw(Matrix view, Matrix projection);

    public void DrawModel(Model model, Matrix world, Matrix view, Matrix projection)
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

    public BoundingBox GetModelSize(Model model)
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
