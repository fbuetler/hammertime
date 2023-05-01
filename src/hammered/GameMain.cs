using System;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public class GameMain : Game
{
    // update order from high to low (default is 0)
    public const int MENU_UPDATE_ORDER = 0;
    public const int MATCH_UPDATE_ORDER = 0;
    public const int MAP_UPDATE_ORDER = 0;
    public const int HUD_UPDATE_ORDER = 0;
    public const int ARROW_UPDATE_ORDER = 1;
    public const int TILE_UPDATE_ORDER = 1;
    public const int HAMMER_UPDATE_ORDER = 2;
    public const int PLAYER_UPDATE_ORDER = 3;
    public const int KILLPLANE_UPDATE_ORDER = 5;

    // draw order from high to low (default is 0)
    public const int MENU_DRAW_ORDER = 0; // clears screen
    public const int MAP_DRAW_ORDER = 0; // clears screen
    public const int KILLPLANE_DRAW_ORDER = 1; // uses whole screen
    public const int TILE_DRAW_ORDER = 2;
    public const int ARROW_DRAW_ORDER = 3;
    public const int HAMMER_DRAW_ORDER = 4;
    public const int PLAYER_DRAW_ORDER = 5;
    public const int OVERLAY_DRAW_ORDER = 6;

    // drawing
    private GraphicsDeviceManager _graphics;

    public SpriteBatch SpriteBatch { get => _spriteBatch; }
    private SpriteBatch _spriteBatch;

    public DebugDraw DebugDraw { get => _debugDraw; }
    private DebugDraw _debugDraw;

    private Menu _menu;

    public Match Match { get => _match; }
    private Match _match;

    public AudioManager AudioManager { get => _audio; }
    private AudioManager _audio;

    public Random Random { get => _random; }
    private Random _random;

    private const string MenuSong = "MenuAudio/Menu";

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    public int GetScreenWidth()
    {
#if DEBUG
        return 1280;
#else
        return 1920;
#endif
    }

    public int GetScreenHeight()
    {
#if DEBUG
        return 720;
#else
        return 1080;
#endif
    }

    public Vector2 GetScreenCenter()
    {
        return new Vector2(
            0.5f * GetScreenWidth(),
            0.5f * GetScreenHeight()
        );
    }

    protected override void Initialize()
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0); // set frame rate to 144 fps
        IsFixedTimeStep = true; // decouple draw from update

        _menu = new Menu(this);
        Components.Add(_menu);

        _audio = new AudioManager(this);

        base.Initialize();

        _graphics.PreferredBackBufferWidth = GetScreenWidth();
        _graphics.PreferredBackBufferHeight = GetScreenHeight(); ;

        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();

        _debugDraw = new DebugDraw(GraphicsDevice);

        _random = new Random();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        InputHelper.Setup(this);

        _audio.LoadSong(MenuSong);
        _audio.PlaySong(MenuSong);
    }

    public void SetupMatch(int numberOfPlayers, int numberOfRounds)
    {
        _match = new Match(this, numberOfPlayers, numberOfRounds);
    }

    public void StartMatch()
    {
        Components.Add(_match);
        Components.Remove(_menu);
    }

    public void EndMatch()
    {
        Components.Clear();
        AudioManager.Stop();

        Components.Add(_menu);
    }

    protected override void Update(GameTime gameTime)
    {
        InputHelper.UpdateSetup();
        base.Update(gameTime);
        InputHelper.UpdateCleanup();
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}
