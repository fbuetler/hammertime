using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
using System.Collections.Generic;

namespace hammered;

public class Map : DrawableGameComponent
{
    public ContentManager Content
    {
        get { return _content; }
    }
    ContentManager _content;

    private GameMain _game;

    private BasicEffect _basicEffect;

    public Camera Camera
    {
        get { return _camera; }
    }
    private Camera _camera;

    private Tile[,] _tiles;

    private Player _player;

    int xBlocks = 15;
    int zBlocks = 10;

    public Map(Game game, IServiceProvider serviceProvider) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (serviceProvider == null)
            throw new ArgumentNullException("serviceProvider");

        _game = (GameMain)game;

        // Create a new content manager to load content used just by this level.
        _content = new ContentManager(serviceProvider, "Content"); // TODO how exactly does this work?

        // setup our graphics scene matrices
        float xMapCenter = xBlocks / 2 * Tile.Size;
        float zMapCenter = zBlocks / 2 * Tile.Size;

        _camera = new Camera(
            new Vector3(xMapCenter, 100f, zMapCenter + 75f),
            new Vector3(xMapCenter, 0f, zMapCenter),
            (float)_game.GetBackBufferWidth() / _game.GetBackBufferHeight()
        );

        // Setup our basic effect
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.World = _camera.WorldMatrix;
        _basicEffect.View = _camera.ViewMatrix;
        _basicEffect.Projection = _camera.ProjectionMatrix;
        _basicEffect.VertexColorEnabled = true;

        _tiles = new Tile[xBlocks, zBlocks];
        for (int x = 0; x < xBlocks; x++)
        {
            for (int z = 0; z < zBlocks; z++)
            {
                _tiles[x, z] = LoadTile(x, 0, z);
            }
        }

        _player = LoadPlayer(0, Tile.Size, 0);
    }

    private Tile LoadTile(float x, float y, float z)
    {
        return new Tile(_game, this, new Vector3(x, y, z));
    }

    private Player LoadPlayer(float x, float y, float z)
    {
        return new Player(_game, this, new Vector3(x, y, z));
    }

    protected override void UnloadContent()
    {
        _content.Unload();
    }

    public override void Update(GameTime gameTime)
    {
        foreach (Tile t in _tiles)
        {
            t.Update(gameTime);
        }
        _player.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        foreach (Tile t in _tiles)
        {
            t.Draw(gameTime);
        }
        _player.Draw(gameTime);
    }
}