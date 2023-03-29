using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace hammered;

public class Map
{
    public ContentManager Content
    {
        get { return _content; }
    }
    ContentManager _content;

    public DebugDraw DebugDraw
    {
        get { return _game.DebugDraw; }
    }

    private GameMain _game;

    private Camera _camera;
    public Camera Camera { get => _camera; }

    public int Width
    {
        get { return _tiles.GetLength(0); }
    }
    public int Height
    {
        get { return _tiles.GetLength(1); }
    }
    public int Depth
    {
        get { return _tiles.GetLength(2); }
    }
    private Tile[,,] _tiles;


    public List<Player> Players
    {
        get { return _players; }
    }


    private List<Player> _players = new List<Player>();

    public Map(Game game, IServiceProvider serviceProvider, Stream fileStream)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (serviceProvider == null)
            throw new ArgumentNullException("serviceProvider");

        _game = (GameMain)game;

        // create a new content manager to load content used just by this map 
        _content = new ContentManager(serviceProvider, "Content");

        // load tiles and players
        LoadTiles(fileStream);

        // setup camera
        _camera = new Camera(
            new Vector3(Width / 2, 0f, Depth / 2),
            (float)_game.GetBackBufferWidth() / _game.GetBackBufferHeight(),
            Width
        );

        LoadMusic();
    }

    private void LoadTiles(Stream fileStream)
    {
        int width;
        List<string> lines = new List<string>();
        using (StreamReader reader = new StreamReader(fileStream))
        {
            string line = reader.ReadLine();
            width = line.Length;
            while (line != null)
            {
                lines.Add(line);
                if (line.Length != width)
                {
                    throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                }
                line = reader.ReadLine();
            }
        }
        int depth = lines.Count;
        int height = 1; // floor TODO (fbuetler) and walls 
        _tiles = new Tile[width, height, lines.Count];

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                char tileType = lines[z][x];
                Tile tile = LoadTile(tileType, x, z);
                _tiles[x, 0, z] = tile; // for now we only load floor tiles
                _game.Components.Add(tile);
            }
        }

        if (_players.Count < _game.NumberOfPlayers)
        {
            throw new NotSupportedException("A map must have starting points for all players");
        }
    }

    private Tile LoadTile(char tileType, int x, int z)
    {
        // TODO (fbuetler) introduce different tile types
        switch (tileType)
        {
            // abyss
            case '.':
                return LoadAbyssTile(x, z);
            // breakable floor
            case '-':
                return LoadFloorTile(x, z);
            // non-breakable floor
            case '#':
                throw new NotSupportedException(String.Format("Tile type '{0}' is not yet supported", tileType));
            // wall
            case 'X':
                throw new NotSupportedException(String.Format("Tile type '{0}' is not yet supported", tileType));
            // player
            case 'P':
                LoadPlayer(x, z);
                return LoadFloorTile(x, z);
            default:
                throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, z));
        }
    }

    private Tile LoadAbyssTile(int x, int z)
    {
        return new Tile(_game, new Vector3(x, 0, z), true);
    }

    private Tile LoadFloorTile(int x, int z)
    {
        return new Tile(_game, new Vector3(x, 0, z), false);
    }

    private void LoadPlayer(int x, int z)
    {
        // ignore starting tiles if already all players are loaded
        if (_players.Count < _game.NumberOfPlayers)
        {
            Player player = LoadPlayer(x, 1, z);
            _players.Add(player);
            _game.Components.Add(player);
        }
    }

    private Player LoadPlayer(float x, float y, float z)
    {
        return new Player(_game, new Vector3(x, y, z), _players.Count);
    }

    private void LoadMusic()
    {
        MediaPlayer.Play(_content.Load<Song>("Audio/Stormfront"));
        MediaPlayer.IsRepeating = true;
    }

    public void Dispose()
    {
        // TODO: (lmeinen) remove components
        _game.Components.Clear();
        _content.Unload();
    }

    public bool HasTile(int x, int y, int z)
    {
        if (x < 0 || x >= Width)
        {
            return false;
        }
        if (y < 0 || y >= Height)
        {
            return false;
        }
        if (z < 0 || z >= Depth)
        {
            return false;
        }

        return _tiles[x, y, z].State != TileState.HP0;
    }

    public BoundingBox GetTileBounds(int x, int y, int z)
    {
        return new BoundingBox(
                new Vector3(x, y, z),
                new Vector3(x + Tile.Width, y + Tile.Height, z + Tile.Depth)
            );
    }

    public Hammer[] GetHammers()
    {
        Hammer[] hammers = new Hammer[_game.NumberOfPlayers];
        for (int i = 0; i < _game.NumberOfPlayers; i++)
        {
            hammers[i] = _players[i].Hammer;
        }
        return hammers;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState[] gamePadStates)
    {
        // IMPORTANT! UpdatePlayers has to be AFTER UpdateTiles because of isPlayerStandingOnAnyTile
        UpdateTiles(gameTime);
    }

    private void UpdateTiles(GameTime gameTime)
    {
        bool[] isPlayerStandingOnAnyTile = new bool[_players.Count];

        // TODO (fbuetler) investigate why sometimes the wrong tiles are breaking
        foreach (Tile t in _tiles)
        {
            t.Update(gameTime);

            if (t.State == TileState.HP0)
            {
                continue;
            }

            for (int j = 0; j < _players.Count; j++)
            {
                Player p = _players[j];

                if (!p.IsAlive)
                {
                    // HACK (fbuetler) a falling player should not interact with tiles
                    continue;
                }

                // is player standing on tile
                if (p.BoundingBox.Intersects(t.BoundingBox))
                {
                    OnTileEnter(t, p);
                    isPlayerStandingOnAnyTile[j] |= true;
                }
                else
                {
                    OnTileExit(t, p);
                }
            }

            if (t.State == TileState.HP0)
            {
                // only called when the tile breaks time
                t.OnBreak();
            }
        }

        // TODO (fbuetler) a bit ugly but is there another efficient way?
        for (int i = 0; i < isPlayerStandingOnAnyTile.Length; i++)
        {
            if (!isPlayerStandingOnAnyTile[i])
            {
                OnPlayerFall(_players[i]);
            }
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

    private void OnPlayerFall(Player player)
    {
        if (player.IsAlive)
        {
            player.OnKilled();
        }
    }

    private void OnPlayerHit(Player player)
    {
        player.OnHit();
    }

    public void Draw(GameTime gameTime)
    {

        Matrix view = Camera.View;
        Matrix projection = Camera.Projection;

#if DEBUG
        // draw coordinate system
        DebugDraw.Begin(Matrix.Identity, view, projection);
        DebugDraw.DrawLine(Vector3.Zero, 30 * Vector3.UnitX, Color.Black);
        DebugDraw.DrawLine(Vector3.Zero, 30 * Vector3.UnitY, Color.Black);
        DebugDraw.DrawLine(Vector3.Zero, 30 * Vector3.UnitZ, Color.Black);
        DebugDraw.End();
#endif
    }
}