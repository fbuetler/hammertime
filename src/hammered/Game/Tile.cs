using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum TileCollision
{
    Passable = 0,

    Impassable = 1,
}

public class Tile : GameObject
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
                new Vector3(_pos.X + Tile.Width, _pos.Y + Tile.Height, _pos.Z + Tile.Depth)
            );
        }
    }

    public bool IsBroken
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
    private Matrix _modelScale;

    public Vector3 Pos
    {
        get { return _pos; }
    }
    private Vector3 _pos;

    public const float Width = 1f;
    public const float Height = 1f;
    public const float Depth = 1f;

    private const float maxHealthPoints = 90f;

    private const float damage = 30f;

    public Tile(Map map, Vector3 position, TileCollision collision)
    {
        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);

        LoadContent();

        Reset(collision);
    }

    public void LoadContent()
    {
        _model = _map.Content.Load<Model>("cube");

        BoundingBox size = GetModelSize(_model);
        float xScale = Width / (size.Max.X - size.Min.X);
        float yScale = Height / (size.Max.Y - size.Min.Y);
        float zScale = Depth / (size.Max.Z - size.Min.Z);
        _modelScale = Matrix.CreateScale(xScale, yScale, zScale);
    }

    public void Reset(TileCollision collision)
    {
        _collision = collision;
        if (_collision == TileCollision.Impassable)
        {
            _healthPoints = maxHealthPoints;
        }
        else
        {
            _healthPoints = 0;
        }


        _visitors = new HashSet<int>();
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState) { /* THIS IS JUST HERE FOR OVERRIDING */ }

    public void Update(GameTime gameTime)
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

    public void OnBreak()
    {
        _collision = TileCollision.Passable;
        // TODO (fbuetler) make invisible i.e. change/remove texture
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        if (IsBroken)
        {
            return;
        }

        Matrix translation = Matrix.CreateTranslation(_pos);

        Matrix world = _modelScale * translation;
        DrawModel(_model, world, view, projection);

        _map.DebugDraw.Begin(Matrix.Identity, view, projection);
        _map.DebugDraw.DrawWireBox(BoundingBox, Color.Black);
        _map.DebugDraw.End();
    }
}