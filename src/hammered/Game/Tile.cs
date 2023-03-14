using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

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
            return new Rectangle((int)_pos.X, (int)_pos.Z, 1, 1);
        }
    }

    public Boolean IsBroken
    {
        get { return _healthPoints <= 0; ; }
    }
    private float _healthPoints;


    private HashSet<int> _visitors;

    private Model _model;

    private Vector3 _pos;

    private const float maxHealthPoints = 100f;

    private const float damage = 10f;

    public Tile(Game game, Map map, Vector3 position) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);
        _model = _map.Content.Load<Model>("RubiksCube");
        // TODO (fbuetler) scale model to 1

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
        _healthPoints = Math.Max(0, _healthPoints - Tile.damage);
    }

    public void OnExit(Player player)
    {
        if (!_visitors.Contains(player.ID))
        {
            return;
        }

        _visitors.Remove(player.ID);
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
}