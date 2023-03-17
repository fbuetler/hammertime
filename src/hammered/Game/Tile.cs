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

    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(
                new Vector3(_pos.X, _pos.Y, _pos.Z),
                new Vector3(_pos.X + Tile.Width, _pos.Y + Tile.Height, _pos.Z + Tile.Width)
            );
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

    public Vector3 Pos
    {
        get { return _pos; }
    }
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
        _map.DebugDraw.DrawWireBox(BoundingBox, Color.Black);
        _map.DebugDraw.End();
    }

    public bool Equals(int x, int z)
    {
        return _pos.X == x && _pos.Z == z;
    }
}