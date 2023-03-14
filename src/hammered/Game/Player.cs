using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace hammered;

public class Player : DrawableGameComponent
{
    public int ID
    {
        get { return _id; }
    }
    int _id;

    public Circle BoundingTopDownCircle
    {
        get
        {
            return new Circle(new Vector2(_pos.X + 0.5f, _pos.Z + 0.5f), 0.5f);
        }
    }

    private Map _map;

    private Model _model;

    private Vector3 _pos;

    private float _speed;

    public Player(Game game, Map map, int id, Vector3 position) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);
        _model = _map.Content.Load<Model>("RubiksCube");
        _id = id;
        _speed = 0.02f;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        Vector3 move = new Vector3(0, 0, 0);
        if (keyboardState.IsKeyDown(Keys.Up))
        {
            move.Z -= _speed;
        }
        else if (keyboardState.IsKeyDown(Keys.Down))
        {
            move.Z += _speed;
        }

        if (keyboardState.IsKeyDown(Keys.Left))
        {
            move.X -= _speed;
        }
        else if (keyboardState.IsKeyDown(Keys.Right))
        {
            move.X += _speed;
        }

        // diagonal moves should not be faster
        if (Math.Abs(move.X) > 0.5 && Math.Abs(move.Z) > 0.5)
        {
            move.Normalize();
        }

        float timepassed = (float)gameTime.ElapsedGameTime.Milliseconds;
        _pos += move;
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix translation = Matrix.CreateTranslation(_pos);

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