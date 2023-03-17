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
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;

            }

            mesh.Draw();
        }
    }

}
