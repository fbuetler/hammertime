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

    private const int TILE_UPDATE_ORDER = 0;
    private const int HAMMER_UPDATE_ORDER = 1;
    private const int PLAYER_UPDATE_ORDER = 2;

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
                tile.UpdateOrder = TILE_UPDATE_ORDER;
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
            case '.':
                // abyss
                return new Tile(_game, new Vector3(x, 0, z), true);
            case '-':
                // breakable floor
                return new Tile(_game, new Vector3(x, 0, z), false);
            case '#':
                // non-breakable floor
                throw new NotSupportedException(String.Format("Tile type '{0}' is not yet supported", tileType));
            case 'X':
                // wall
                throw new NotSupportedException(String.Format("Tile type '{0}' is not yet supported", tileType));
            case 'P':
                // player
                LoadPlayer(x, z);
                // breakable floor
                return new Tile(_game, new Vector3(x, 0, z), false);
            default:
                throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, z));
        }
    }

    private void LoadPlayer(int x, int z)
    {
        // ignore starting tiles if already all players are loaded
        if (_players.Count < _game.NumberOfPlayers)
        {
            Player player = LoadPlayer(x, 1, z);
            _players.Add(player);

            // enable player component
            player.UpdateOrder = PLAYER_UPDATE_ORDER;
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

    public BoundingBox? GetTileBounds(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Depth || _tiles[x, y, z].State == TileState.HP0)
        {
            return null;
        }
        else
        {
            return _tiles[x, y, z].BoundingBox;
        }
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