using System;
using System.Collections.Generic;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
                if (Controls.Start.Pressed())
                    _state = MenuState.MAIN_START;
                break;

            // main
            case MenuState.MAIN_START:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.PLAYERS_2;
                break;
            case MenuState.MAIN_SETTINGS:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.SETTINGS;
                break;
            case MenuState.MAIN_QUIT:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.QUIT_YES;
                break;

            // players
            case MenuState.PLAYERS_2:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_4;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.PLAYERS_3;
                else if (Controls.Interact.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    StartMap(2);
                }
                break;
            case MenuState.PLAYERS_3:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_2;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.PLAYERS_4;
                else if (Controls.Interact.Pressed())
                {
                    _state = MenuState.MAIN_START;
                    StartMap(3);
                }
                break;
            case MenuState.PLAYERS_4:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.PLAYERS_3;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.PLAYERS_2;
                else if (Controls.Interact.Pressed())
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
                if (Controls.FocusPrev.Pressed() || Controls.FocusNext.Pressed())
                    _state = MenuState.QUIT_NO;
                else if (Controls.Interact.Pressed())
                    GameMain.Exit();
                break;
            case MenuState.QUIT_NO:
                if (Controls.FocusPrev.Pressed() || Controls.FocusNext.Pressed())
                    _state = MenuState.QUIT_YES;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.MAIN_START;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), $"Unexpected menu state: {_state}");
        }

        if (Controls.Back.Pressed())
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