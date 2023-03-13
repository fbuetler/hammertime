using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace hammered;

public class Tile : DrawableGameComponent
{
    public Map Map
    {
        get { return _map; }
    }
    Map _map;

    Model _model;

    public static float Size = 9.8f;

    Vector3 _pos;

    public Tile(Game game, Map map, Vector3 position) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);
        _model = _map.Content.Load<Model>("RubiksCube");
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix translation = Matrix.CreateTranslation(_pos * Size);

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.AmbientLightColor = new Vector3(1f, 0, 0);
                effect.World = _map.Camera.WorldMatrix;

                // translate tiles
                Matrix translatedView = new Matrix();
                Matrix viewMatrix = _map.Camera.ViewMatrix;
                Matrix.Multiply(ref translation, ref viewMatrix, out translatedView);
                effect.View = translatedView;

                effect.Projection = _map.Camera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }
}