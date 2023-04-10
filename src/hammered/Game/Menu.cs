using System;
using System.Collections.Generic;
using Apos.Input;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

enum MenuState
{
    TITLE,

    MAIN_START,
    MAIN_SETTINGS,
    MAIN_QUIT,

    PLAYERS_2,
    PLAYERS_3,
    PLAYERS_4,

    SETTINGS, // TODO (fbuetler) subcategories

    QUIT_YES,
    QUIT_NO,
};

public class Menu : DrawableGameComponent
{
    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private MenuState _state = MenuState.TITLE;

    private Dictionary<MenuState, string> _menuPaths;

    public Dictionary<string, Texture2D> Menus { get => _menus; }
    private Dictionary<string, Texture2D> _menus;

    public static int PlayerIndex = 0;

    public static ICondition Start { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, PlayerIndex),
            new GamePadCondition(GamePadButton.Start, PlayerIndex)
        );

    public static ICondition Back { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Escape),
            new GamePadCondition(GamePadButton.B, PlayerIndex),
            new GamePadCondition(GamePadButton.Back, PlayerIndex)
        );

    public static ICondition Interact { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, PlayerIndex)
        );

    public static ICondition FocusPrev { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Up),
            new AllCondition(
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftShift),
                    new KeyboardCondition(Keys.RightShift)
                ),
                new KeyboardCondition(Keys.Tab)
            ),
            new GamePadCondition(GamePadButton.Up, PlayerIndex)
        );

    public static ICondition FocusNext { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Down),
            new KeyboardCondition(Keys.Tab),
            new GamePadCondition(GamePadButton.Down, PlayerIndex)
        );

    public static ICondition MoveLeft { get; set; } =
    new AnyCondition(
        new KeyboardCondition(Keys.Left),
        new GamePadCondition(GamePadButton.Left, PlayerIndex)
    );

    public static ICondition MoveRight { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Right),
            new GamePadCondition(GamePadButton.Right, PlayerIndex)
        );

    public Menu(Game game) : base(game)
    {
        _game = (GameMain)game;

        _menuPaths = new Dictionary<MenuState, string>();
        _menuPaths[MenuState.TITLE] = "Menu/title";
        _menuPaths[MenuState.MAIN_START] = "Menu/Main/main_start";
        _menuPaths[MenuState.MAIN_SETTINGS] = "Menu/Main/main_settings";
        _menuPaths[MenuState.MAIN_QUIT] = "Menu/Main/main_quit";
        _menuPaths[MenuState.PLAYERS_2] = "Menu/Players/players_2";
        _menuPaths[MenuState.PLAYERS_3] = "Menu/Players/players_3";
        _menuPaths[MenuState.PLAYERS_4] = "Menu/Players/players_4";
        _menuPaths[MenuState.SETTINGS] = "Menu/Settings/settings";
        _menuPaths[MenuState.QUIT_YES] = "Menu/Quit/quit_yes";
        _menuPaths[MenuState.QUIT_NO] = "Menu/Quit/quit_no";

        _menus = new Dictionary<string, Texture2D>();
    }

    protected override void LoadContent()
    {
        foreach (MenuState state in Enum.GetValues(typeof(MenuState)))
        {
            if (!Menus.ContainsKey(state.ToString()))
            {
                var texture = GameMain.Content.Load<Texture2D>(_menuPaths[state]);
                Menus.Add(state.ToString(), texture);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        switch (_state)
        {
            // title
            case MenuState.TITLE:
                if (Start.Pressed())
                    _state = MenuState.MAIN_START;
                break;

            // main
            case MenuState.MAIN_START:
                if (FocusPrev.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (FocusNext.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
                else if (Interact.Pressed())
                    _state = MenuState.PLAYERS_2;
                break;
            case MenuState.MAIN_SETTINGS:
                if (FocusPrev.Pressed())
                    _state = MenuState.MAIN_START;
                else if (FocusNext.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Interact.Pressed())
                    _state = MenuState.SETTINGS;
                break;
            case MenuState.MAIN_QUIT:
                if (FocusPrev.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
                else if (FocusNext.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Interact.Pressed())
                    _state = MenuState.QUIT_YES;
                break;

            // players
            case MenuState.PLAYERS_2:
                if (FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_4;
                else if (FocusNext.Pressed())
                    _state = MenuState.PLAYERS_3;
                else if (Interact.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    StartMap(2);
                }
                break;
            case MenuState.PLAYERS_3:
                if (FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_2;
                else if (FocusNext.Pressed())
                    _state = MenuState.PLAYERS_4;
                else if (Interact.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    StartMap(3);
                }
                break;
            case MenuState.PLAYERS_4:
                if (FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_3;
                else if (FocusNext.Pressed())
                    _state = MenuState.PLAYERS_2;
                else if (Interact.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    StartMap(4);
                }
                break;

            // settings
            case MenuState.SETTINGS:
                break;

            // quit
            case MenuState.QUIT_YES:
                if (FocusPrev.Pressed() || FocusNext.Pressed())
                    _state = MenuState.QUIT_NO;
                else if (Interact.Pressed())
                    GameMain.Exit();
                break;
            case MenuState.QUIT_NO:
                if (FocusPrev.Pressed() || FocusNext.Pressed())
                    _state = MenuState.QUIT_YES;
                else if (Interact.Pressed())
                    _state = MenuState.MAIN_START;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), $"Unexpected menu state: {_state}");
        }

        if (Back.Pressed())
        {
            if (_state == MenuState.MAIN_START)
            {
                _state = MenuState.MAIN_QUIT;
            }
            else
            {
                _state = MenuState.MAIN_START;
            }
        }

        base.Update(gameTime);
    }

    private void StartMap(int NumberOfPlayers)
    {

    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        _game.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        var texture = Menus[_state.ToString()];
        Vector2 scale = new Vector2(
            GameMain.GetBackBufferWidth() / (float)texture.Width,
            GameMain.GetBackBufferHeight() / (float)texture.Height
        );
        GameMain.SpriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        _game.SpriteBatch.End();

        base.Draw(gameTime);
    }
}