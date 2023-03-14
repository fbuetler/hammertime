using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    private List<Tile> _tiles;

    private List<Player> _players;

    private int xBlocks = 15;
    private int zBlocks = 10;

    private int nextPlayerID = 0;

    public Map(Game game, IServiceProvider serviceProvider) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (serviceProvider == null)
            throw new ArgumentNullException("serviceProvider");

        _game = (GameMain)game;

        // create a new content manager to load content used just by this map 
        _content = new ContentManager(serviceProvider, "Content"); // TODO (fbuetler) how exactly does this work?

        // setup our graphics scene matrices
        float xMapCenter = xBlocks / 2;
        float zMapCenter = zBlocks / 2;

        _camera = new Camera(
            new Vector3(xMapCenter, 10f, zMapCenter + 7f),
            new Vector3(xMapCenter, 0f, zMapCenter),
            (float)_game.GetBackBufferWidth() / _game.GetBackBufferHeight()
        );

        // Setup our basic effect
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.World = _camera.WorldMatrix;
        _basicEffect.View = _camera.ViewMatrix;
        _basicEffect.Projection = _camera.ProjectionMatrix;
        _basicEffect.VertexColorEnabled = true;

        _tiles = new List<Tile>();
        for (int x = 0; x < xBlocks; x++)
        {
            for (int z = 0; z < zBlocks; z++)
            {
                _tiles.Add(LoadTile(x, 0, z));
            }
        }

        _players = new List<Player>();
        _players.Add(LoadPlayer(0, 1, 0)); // TODO (fbuetler) more players (with respective input)
    }

    private Tile LoadTile(int x, int y, int z)
    {
        return new Tile(_game, this, new Vector3(x, y, z));
    }

    private Player LoadPlayer(float x, float y, float z)
    {
        return new Player(_game, this, nextPlayerID++, new Vector3(x, y, z));
    }

    protected override void UnloadContent()
    {
        _content.Unload();
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        // IMPORTANT! UpdatePlayers has to be AFTER UpdateTiles because of isPlayerStandingOnAnyTile
        UpdateTiles(gameTime);
        UpdatePlayers(gameTime, keyboardState, gamePadState);
    }

    private void UpdatePlayers(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            Player p = _players[i];
            p.Update(gameTime, keyboardState, gamePadState);
        }
    }

    private void UpdateTiles(GameTime gameTime)
    {
        Boolean[] isPlayerStandingOnAnyTile = new Boolean[_players.Count];

        for (int i = 0; i < _tiles.Count; i++)
        {
            Tile t = _tiles[i];

            t.Update(gameTime);

            for (int j = 0; j < _players.Count; j++)
            {
                Player p = _players[j];

                // is player standing on tile
                if (p.BoundingTopDownCircle.Intersects(t.BoundingTopDownRectangle))
                {
                    OnTileEnter(t, p);
                }
                else
                {
                    OnTileExit(t, p); // TODO (fbuetler) can we optimize this?
                }

                // is most part of the player standing on the tile
                float depth = p.BoundingTopDownCircle.GetIntersectionDepth(t.BoundingTopDownRectangle);
                if (0 <= depth && depth <= p.BoundingTopDownCircle.Radius)
                {
                    isPlayerStandingOnAnyTile[j] |= true;
                }
            }

            if (t.IsBroken)
            {
                _tiles.RemoveAt(i--);
            }
        }

        // TODO (fbuetler) a bit ugly but is there another efficient way?
        for (int i = 0; i < isPlayerStandingOnAnyTile.Length; i++)
        {
            _players[i].IsStandingOnTile = isPlayerStandingOnAnyTile[i];
        }
    }

    private void OnTileEnter(Tile tile, Player player)
    {
        tile.OnEnter(player);
    }

    private void OnTileExit(Tile tile, Player player)
    {
        tile.OnExit(player);
    }

    public override void Draw(GameTime gameTime)
    {
        foreach (Player p in _players)
        {
            p.Draw(gameTime);
        }
        foreach (Tile t in _tiles)
        {
            t.Draw(gameTime);
        }
    }
}