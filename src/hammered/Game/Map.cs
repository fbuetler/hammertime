using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace hammered;

public class Map : DrawableGameComponent
{

    public ContentManager Content { get => _content; }
    ContentManager _content;

    public DebugDraw DebugDraw { get => _game.DebugDraw; }

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    public Camera Camera { get => _camera; }
    private Camera _camera;

    private String _mapPath;

    public int Width { get => _tiles.GetLength(0); }
    public int Height { get => _tiles.GetLength(1); }
    public int Depth { get => _tiles.GetLength(2); }
    public Tile[,,] Tiles { get => _tiles; }
    private Tile[,,] _tiles;

    public Dictionary<int, Player> Players { get => _players; }
    private Dictionary<int, Player> _players = new Dictionary<int, Player>();

    public Dictionary<int, Hammer> Hammers { get => _hammers; }
    private Dictionary<int, Hammer> _hammers = new Dictionary<int, Hammer>();

    public Map(Game game, IServiceProvider serviceProvider, String mapPath) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (serviceProvider == null)
            throw new ArgumentNullException("serviceProvider");

        _game = (GameMain)game;

        // create a new content manager to load content used just by this map 
        _content = new ContentManager(serviceProvider, "Content");

        _mapPath = mapPath;

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.MAP_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.MAP_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        using (Stream fileStream = TitleContainer.OpenStream(_mapPath))
            LoadMap(fileStream);
        LoadCamera();
        LoadMusic();
    }

    private void LoadMap(Stream fileStream)
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
        int height = 2; // floor and walls

        _tiles = new Tile[width, height, depth];
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char tileType = lines[z][x];
                    _tiles[x, y, z] = LoadTile(tileType, x, y, z);
                }
            }
        }

        if (_players.Count < GameMain.Match.NumberOfPlayers)
        {
            throw new NotSupportedException("A map must have starting points for all players");
        }
    }

    private Tile LoadTile(char tileType, int x, int y, int z)
    {
        // TODO (fbuetler) introduce different tile types
        Tile tile;
        switch (tileType)
        {
            case '.' when y == 0:
                return null;
            case '-' when y == 0:
                tile = LoadBreakableFloorTile(x, y, z);
                break;
            case '#' when y == 0:
                tile = LoadNonBreakableFloorTile(x, y, z);
                break;
            case 'P' when y == 0:
                LoadPlayer(x, 1, z);
                tile = LoadBreakableFloorTile(x, y, z);
                break;
            case 'W' when y == 0:
                tile = LoadBreakableFloorTile(x, y, z);
                break;
            case 'W' when y == 1:
                tile = LoadWallTile(x, y, z);
                break;
            default:
                return null;
        }
        GameMain.Components.Add(tile);
        return tile;
    }

    private Tile LoadBreakableFloorTile(int x, int y, int z)
    {
        return new Tile(GameMain, new Vector3(x, y, z));
    }

    private Tile LoadNonBreakableFloorTile(int x, int y, int z)
    {
        throw new NotSupportedException(String.Format("Tile type 'non breakable floor' is not yet supported"));
    }

    private Tile LoadWallTile(int x, int y, int z)
    {
        return new Tile(GameMain, new Vector3(x, y, z));
    }

    private void LoadPlayer(int x, int y, int z)
    {
        // ignore starting tiles if already all players are loaded
        if (_players.Count < GameMain.Match.NumberOfPlayers)
        {
            int playerId = _players.Count;
            Player player = new Player(GameMain, new Vector3(x, y, z), playerId);
            Hammer hammer = new Hammer(GameMain, new Vector3(x, y, z), playerId);
            _players.Add(playerId, player);
            _hammers.Add(playerId, hammer);

            // enable player component
            GameMain.Components.Add(player);

            // enable hammer compohent
            GameMain.Components.Add(hammer);
        }
    }

    private void LoadMusic()
    {
        // game crashes sometimes with:
        // Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
        // are we loading not fast enough?
        try
        {
            Song storm = Content.Load<Song>("Audio/MusicMapSlow");
            MediaPlayer.Play(storm);
            MediaPlayer.IsRepeating = true;
           
            
        }
        catch { }
    }

    private void LoadCamera()
    {
        _camera = new Camera(
            new Vector3(Width / 2, 0f, Depth / 2),
            (float)GameMain.GetBackBufferWidth() / GameMain.GetBackBufferHeight(),
            Width
        );
    }

    public BoundingBox? TryGetTileBounds(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 ||
            x >= Width || y >= Height || z >= Depth ||
            _tiles[x, y, z] == null ||
            _tiles[x, y, z].State == TileState.HP0)
        {
            return null;
        }
        else
        {
            return _tiles[x, y, z].BoundingBox;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

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