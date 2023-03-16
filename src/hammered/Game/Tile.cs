using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public enum TileCollision
{
    Passable = 0,

    Impassable = 1,
}

public class Tile : DrawableGameComponent
{
    public Map Map
    {
        get { return _map; }
    }
    Map _map;

    public Rectangle BoundingTopDownRectangle
    {
        get
        {
            return new Rectangle((int)_pos.X, (int)_pos.Z, Tile.Width, Tile.Height);
        }
    }

    public Boolean IsBroken
    {
        get { return _healthPoints <= 0; }
    }
    private float _healthPoints;

    public TileCollision Collision
    {
        get { return _collision; }
    }
    private TileCollision _collision;

    private HashSet<int> _visitors;

    private Model _model;

    private Vector3 _pos;

    public const int Width = 1;
    public const int Height = 1;

    private const float maxHealthPoints = 100f;

    private const float damage = 10f;

    public Tile(Game game, Map map, Vector3 position, TileCollision collision) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);
        _collision = collision;

        // TODO (fbuetler) scale model to 1
        _model = _map.Content.Load<Model>("RubiksCube");

        _healthPoints = maxHealthPoints;
        _visitors = new HashSet<int>();
    }

    public override void Update(GameTime gameTime)
    {
        // TODO (fbuetler) update breaking animation based on health points
    }

    public void OnEnter(Player player)
    {
        if (_visitors.Contains(player.ID))
        {
            return;
        }

        _visitors.Add(player.ID);
    }

    public void OnExit(Player player)
    {
        if (!_visitors.Contains(player.ID))
        {
            return;
        }

        _visitors.Remove(player.ID);
        _healthPoints = Math.Max(0, _healthPoints - Tile.damage);
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix translation = Matrix.CreateTranslation(_pos * 1);

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

    public bool Equals(int x, int z)
    {
        return _pos.X == x && _pos.Z == z;
    }
}