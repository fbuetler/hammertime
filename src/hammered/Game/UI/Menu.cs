using System;
using System.Collections.Generic;
using System.Linq;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

enum MenuState
{
    TITLE,

    MAIN_START,
    MAIN_OPTIONS,
    MAIN_QUIT,

    PLAYERS,
    PLAYERS_CONFIRMED,

    ROUNDS_1,
    ROUNDS_3,
    ROUNDS_5,
    ROUNDS_10,

    // IMPORTANT: make sure the countdowns are in order
    COUNTDOWN_0,
    COUNTDOWN_1,
    COUNTDOWN_2,
    COUNTDOWN_3,

    // IMPORTANT: make sure the volumes are in order
    OPTIONS_VOLUME_0,
    OPTIONS_VOLUME_1,
    OPTIONS_VOLUME_2,
    OPTIONS_VOLUME_3,
    OPTIONS_VOLUME_4,
    OPTIONS_VOLUME_5,

    QUIT_YES,
    QUIT_NO,
};

record MenuGroup(List<(int, MenuState)> buttons, Vector2 anchor);

record MenuComponent(string path, Texture2D texture);

public class Menu : DrawableGameComponent
{
    // button grid in texture
    private static float BUTTON_GRID_HORIZONTAL_ANCHOR = 412;
    private static float BUTTON_GRID_VERTICAL_ANCHOR = 0;
    private static float BUTTON_GRID_HORIZONTAL_OFFSET = 190;
    private static float BUTTON_GRID_VERTICAL_OFFSET = 56;

    private static float BUTTON_GRID_ACTIVE_COL = 0;
    private static float BUTTON_GRID_INACTIVE_COL = 1;

    private static Vector2 BUTTON_SIZE = new Vector2(176, 55);
    private static float MARGIN = 2f;

    private static int MAIN_START_ROW = 0;
    private static int MAIN_OPTIONS_ROW = 1;
    private static int MAIN_QUIT_ROW = 2;
    private static int QUIT_YES_ROW = 3;
    private static int QUIT_NO_ROW = 4;

    private static Color TEXT_COLOR = Color.Black;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private MenuState _state = MenuState.TITLE;

    private Texture2D _title;
    private Texture2D _menuItems;

    private SpriteFont _impactFont;

    private MenuGroup _startMenu;
    private MenuGroup _quitMenu;
    private MenuGroup _playersMenu;

    private int _playersConnected;
    private List<bool> _playersConfirmed;

    private Dictionary<MenuState, MenuComponent> _menus;
    private Texture2D[] _confirmedTextures;
    private Texture2D[] _unconfirmedTextures;

    private float _countdownElapsedMs = 0;

    private static readonly Vector2 nextLineOffset = new Vector2(0, 50);

    // textures
    private const string confirmedTexturePrefix = "Menu/Players/confirmed";
    private const string unconfirmedTexturePrefix = "Menu/Players/unconfirmed";

    // sound effects
    public const string InteractButtonPressSoundEffect = "MenuAudio/ButtonPress0";
    public const string AlternativeButtonPressSoundEffect = "MenuAudio/ButtonPress1";

    // sound
    private const float volumeStep = 1f / (MenuState.OPTIONS_VOLUME_5 - MenuState.OPTIONS_VOLUME_0);

    public Menu(Game game) : base(game)
    {
        _game = (GameMain)game;

        _menus = new Dictionary<MenuState, MenuComponent>();
        _menus[MenuState.TITLE] = new MenuComponent("Menu/title", null);

        _menus[MenuState.MAIN_START] = new MenuComponent("Menu/Start/press_start", null);
        _menus[MenuState.MAIN_OPTIONS] = new MenuComponent("Menu/Start/press_options", null);
        _menus[MenuState.MAIN_QUIT] = new MenuComponent("Menu/Start/press_quit", null);

        _menus[MenuState.PLAYERS] = new MenuComponent("Menu/Players/unconfirmed/background", null);
        _menus[MenuState.PLAYERS_CONFIRMED] = new MenuComponent("Menu/Players/confirmed/background", null);

        _menus[MenuState.ROUNDS_1] = new MenuComponent("Menu/Goals/goal_1", null);
        _menus[MenuState.ROUNDS_3] = new MenuComponent("Menu/Goals/goal_3", null);
        _menus[MenuState.ROUNDS_5] = new MenuComponent("Menu/Goals/goal_5", null);
        _menus[MenuState.ROUNDS_10] = new MenuComponent("Menu/Goals/goal_10", null);

        _menus[MenuState.COUNTDOWN_0] = new MenuComponent("Menu/Countdown/0", null);
        _menus[MenuState.COUNTDOWN_1] = new MenuComponent("Menu/Countdown/1", null);
        _menus[MenuState.COUNTDOWN_2] = new MenuComponent("Menu/Countdown/2", null);
        _menus[MenuState.COUNTDOWN_3] = new MenuComponent("Menu/Countdown/3", null);

        _menus[MenuState.OPTIONS_VOLUME_0] = new MenuComponent("Menu/Options/volume0", null);
        _menus[MenuState.OPTIONS_VOLUME_1] = new MenuComponent("Menu/Options/volume1", null);
        _menus[MenuState.OPTIONS_VOLUME_2] = new MenuComponent("Menu/Options/volume2", null);
        _menus[MenuState.OPTIONS_VOLUME_3] = new MenuComponent("Menu/Options/volume3", null);
        _menus[MenuState.OPTIONS_VOLUME_4] = new MenuComponent("Menu/Options/volume4", null);
        _menus[MenuState.OPTIONS_VOLUME_5] = new MenuComponent("Menu/Options/volume5", null);

        _menus[MenuState.QUIT_YES] = new MenuComponent("Menu/Quit/quit_yes", null);
        _menus[MenuState.QUIT_NO] = new MenuComponent("Menu/Quit/quit_no", null);

        _confirmedTextures = new Texture2D[Match.MaxNumberOfPlayers];
        _unconfirmedTextures = new Texture2D[Match.MaxNumberOfPlayers];

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.MENU_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.MENU_DRAW_ORDER;

        _playersConfirmed = new List<bool>(new bool[Match.MaxNumberOfPlayers]);
    }

    protected override void LoadContent()
    {
        _title = GameMain.Content.Load<Texture2D>("Menu/title");
        _menuItems = GameMain.Content.Load<Texture2D>("Menu/items");
        _impactFont = _game.Content.Load<SpriteFont>("Fonts/impact");

        LoadPlayersMenuGroup();

        foreach (MenuState state in Enum.GetValues(typeof(MenuState)))
        {
            string path = _menus[state].path;
            var texture = GameMain.Content.Load<Texture2D>(path);
            _menus[state] = new MenuComponent(path, texture);
        }

        for (int i = 0; i < _confirmedTextures.Length; i++)
        {
            _confirmedTextures[i] = GameMain.Content.Load<Texture2D>($"{confirmedTexturePrefix}/{i}");
        }
        for (int i = 0; i < _unconfirmedTextures.Length; i++)
        {
            _unconfirmedTextures[i] = GameMain.Content.Load<Texture2D>($"{unconfirmedTexturePrefix}/{i}");
        }

        GameMain.AudioManager.LoadSoundEffect(InteractButtonPressSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(AlternativeButtonPressSoundEffect);
    }

    private void LoadStartMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (MAIN_START_ROW, MenuState.MAIN_START),
            (MAIN_OPTIONS_ROW, MenuState.MAIN_OPTIONS),
            (MAIN_QUIT_ROW, MenuState.MAIN_QUIT)
        };
        _startMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count, Vector2.Zero));
    }

    private void LoadQuitMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (QUIT_YES_ROW, MenuState.QUIT_YES),
            (QUIT_NO_ROW, MenuState.QUIT_NO),
        };
        _quitMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count, Vector2.Zero));
    }

    private void LoadPlayersMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (MAIN_START_ROW, MenuState.PLAYERS_CONFIRMED),
        };
        _playersMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count, new Vector2(0, 150)));
    }

    private Vector2 CalculateAnchorOfMenuGroup(int numberOfButons, Vector2 centerOffset)
    {
        // TODO (fbuetler) distinguish even (center is between buttons) and odd number of buttons (center is on a button)
        float totalHeight = numberOfButons * BUTTON_SIZE.Y + (numberOfButons - 1) * MARGIN;
        Vector2 anchor = GameMain.GetScreenCenter();
        anchor.Y -= totalHeight * 0.5f;

        return anchor + centerOffset;
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

        MenuState prev = _state;
        switch (_state)
        {
            // title
            case MenuState.TITLE:
                if (Controls.Start.Pressed())
                    _state = MenuState.MAIN_START;
                break;

            // main
            case MenuState.MAIN_OPTIONS:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.Interact.Pressed())
                    // very hacky, but Im too lazy for a switch statement. take this comment instead
                    _state = MenuState.OPTIONS_VOLUME_0 + GetVolumeLevel();
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;
            case MenuState.MAIN_START:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_OPTIONS;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.PLAYERS;
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;
            case MenuState.MAIN_QUIT:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_OPTIONS;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.QUIT_YES;
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;

            // players
            case MenuState.PLAYERS:
                if (Controls.Back.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    break;
                }

                _playersConnected = Controls.ConnectedPlayers();
                for (int i = 0; i < _playersConnected; i++)
                {
                    if (Controls.InteractP(i).Pressed() && !_playersConfirmed[i])
                    {
                        _playersConfirmed[i] = true;
                    }
                    else if (Controls.BackP(i).Pressed() && _playersConfirmed[i])
                    {
                        _playersConfirmed[i] = false;
                    }
                }

                if (_playersConfirmed.Take(_playersConnected).All(p => p))
                {
                    _state = MenuState.PLAYERS_CONFIRMED;
                }

                break;
            case MenuState.PLAYERS_CONFIRMED:
                _playersConnected = Controls.ConnectedPlayers();
                for (int i = 0; i < _playersConnected; i++)
                {
                    if (Controls.InteractP(i).Pressed() && _playersConnected > 1)
                    {
                        _state = MenuState.ROUNDS_5;
                    }
                    else if (Controls.BackP(i).Pressed() && _playersConfirmed[i])
                    {
                        _playersConfirmed[i] = false;
                        _state = MenuState.PLAYERS;
                    }
                }

#if DEBUG
                if (Controls.Start.Pressed())
                {
                    // helper if a poor, controller-less peasant (lasse) needs to test the game with a keyboard
                    _state = MenuState.ROUNDS_5;
                    _playersConnected = 2;
                }
#endif
                break;

            case MenuState.ROUNDS_1:
            case MenuState.ROUNDS_3:
            case MenuState.ROUNDS_5:
            case MenuState.ROUNDS_10:
                if (Controls.Back.Pressed())
                    _state = MenuState.PLAYERS_CONFIRMED;
                else if (Controls.Increase.Pressed())
                {
                    // very hacky
                    _state = (MenuState)MathHelper.Clamp(
                        (float)_state + 1,
                        (float)MenuState.ROUNDS_1,
                        (float)MenuState.ROUNDS_10
                    );
                }
                else if (Controls.Decrease.Pressed())
                {
                    // very hacky
                    // very hacky
                    _state = (MenuState)MathHelper.Clamp(
                        (float)_state - 1,
                        (float)MenuState.ROUNDS_1,
                        (float)MenuState.ROUNDS_10
                    );
                }
                else if (Controls.Interact.Pressed())
                {
                    switch (_state)
                    {
                        case MenuState.ROUNDS_1:
                            GameMain.SetupMatch(_playersConnected, 1);
                            break;
                        case MenuState.ROUNDS_3:
                            GameMain.SetupMatch(_playersConnected, 3);
                            break;
                        case MenuState.ROUNDS_5:
                            GameMain.SetupMatch(_playersConnected, 5);
                            break;
                        case MenuState.ROUNDS_10:
                            GameMain.SetupMatch(_playersConnected, 10);
                            break;
                    }
                    _state = MenuState.COUNTDOWN_3;
                }
                break;

            case MenuState.COUNTDOWN_3:
            case MenuState.COUNTDOWN_2:
            case MenuState.COUNTDOWN_1:
                _countdownElapsedMs += elapsed;
                if (_countdownElapsedMs > 1000)
                {
                    _state = _state - 1;
                    _countdownElapsedMs = 0;
                }
                break;
            case MenuState.COUNTDOWN_0:
                _countdownElapsedMs += elapsed;
                if (_countdownElapsedMs > 1000)
                {
                    GameMain.StartMatch();
                    _state = MenuState.MAIN_START; // prepare post match menu state
                    _countdownElapsedMs = 0;
                }
                break;

            // options
            case MenuState.OPTIONS_VOLUME_0:
            case MenuState.OPTIONS_VOLUME_1:
            case MenuState.OPTIONS_VOLUME_2:
            case MenuState.OPTIONS_VOLUME_3:
            case MenuState.OPTIONS_VOLUME_4:
            case MenuState.OPTIONS_VOLUME_5:
                if (Controls.Back.Pressed())
                    _state = MenuState.MAIN_OPTIONS;
                else if (Controls.Increase.Pressed())
                {
                    // very hacky
                    SetVolumeLevel(_state - MenuState.OPTIONS_VOLUME_0 + 1);
                    _state = MenuState.OPTIONS_VOLUME_0 + GetVolumeLevel();
                }
                else if (Controls.Decrease.Pressed())
                {
                    // very hacky
                    SetVolumeLevel(_state - MenuState.OPTIONS_VOLUME_0 - 1);
                    _state = MenuState.OPTIONS_VOLUME_0 + GetVolumeLevel();
                }
                break;

            // quit
            case MenuState.QUIT_YES:
                if (Controls.FocusPrev.Pressed() || Controls.FocusNext.Pressed())
                    _state = MenuState.QUIT_NO;
                else if (Controls.Interact.Pressed())
                    GameMain.Exit();
                else if (Controls.Back.Pressed())
                    _state = MenuState.MAIN_QUIT;
                break;
            case MenuState.QUIT_NO:
                if (Controls.FocusPrev.Pressed() || Controls.FocusNext.Pressed())
                    _state = MenuState.QUIT_YES;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.Back.Pressed())
                    _state = MenuState.MAIN_QUIT;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), $"Unexpected menu state: {_state}");
        }

        if (_state != prev && prev != MenuState.COUNTDOWN_0)
        {
            if (Controls.Interact.Pressed())
            {
                GameMain.AudioManager.PlaySoundEffect(InteractButtonPressSoundEffect);
            }
            else
            {
                GameMain.AudioManager.PlaySoundEffect(AlternativeButtonPressSoundEffect);
            }
        }
    }

    private int GetVolumeLevel()
    {
        float volume = GameMain.AudioManager.SongVolume;
        int level = (int)MathF.Floor(volume / volumeStep);
        return level;
    }

    private void SetVolumeLevel(int level)
    {
        float volume = level * volumeStep;
        GameMain.AudioManager.SongVolume = volume;
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        switch (_state)
        {
            case MenuState.PLAYERS:
            case MenuState.PLAYERS_CONFIRMED:
                DrawPlayerScreen(GameMain.SpriteBatch);
                break;
            default:
                DrawFullScreen(GameMain.SpriteBatch, _menus[_state].texture);
                break;
        }

        GameMain.SpriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawPlayerScreen(SpriteBatch spriteBatch)
    {
        DrawFullScreen(GameMain.SpriteBatch, _menus[_state].texture);

        Vector2 screenCenter = GameMain.GetScreenCenter();
        Vector2 anchor = screenCenter - new Vector2(0, GameMain.GetScreenHeight() / 6);
        for (int i = 0; i < _playersConnected; i++)
        {
            Texture2D t;
            if (!_playersConfirmed[i])
            {
                t = _unconfirmedTextures[i];
            }
            else
            {
                t = _confirmedTextures[i];
            }
            DrawFullScreen(GameMain.SpriteBatch, t);
        }
    }

    private void DrawFullScreen(SpriteBatch spriteBatch, Texture2D texture)
    {
        Vector2 scale = new Vector2(
                    GameMain.GetScreenWidth() / (float)texture.Width,
                    GameMain.GetScreenHeight() / (float)texture.Height
                );
        spriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawMenuGroup(SpriteBatch spriteBatch, MenuGroup group)
    {
        for (int i = 0; i < group.buttons.Count; i++)
        {
            var b = group.buttons[i];

            Rectangle area = new Rectangle(
                new Vector2(
                    BUTTON_GRID_HORIZONTAL_ANCHOR + (_state == b.Item2 ? BUTTON_GRID_ACTIVE_COL : BUTTON_GRID_INACTIVE_COL) * BUTTON_GRID_HORIZONTAL_OFFSET,
                    BUTTON_GRID_VERTICAL_ANCHOR + b.Item1 * BUTTON_GRID_VERTICAL_OFFSET
                ).ToPoint(),
                BUTTON_SIZE.ToPoint()
            );

            Vector2 position = group.anchor - BUTTON_SIZE * 0.5f;
            position.Y += (BUTTON_SIZE.Y + MARGIN) * i;
            spriteBatch.Draw(
                _menuItems,
                position: position,
                sourceRectangle: area,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }

    private void DrawString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 anchor)
    {
        Vector2 textSize = font.MeasureString(text);
        Vector2 position = anchor - textSize * 0.5f;
        position.Y = position.Y - BUTTON_SIZE.Y * 0.5f - MARGIN - textSize.Y * 0.5f;

        spriteBatch.DrawString(font, text, position, TEXT_COLOR);
    }
}