using System;
using System.Collections.Generic;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// TODO (fbuetler) maybe do a hierachical state machine
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

record MenuGroup(List<(int, MenuState)> buttons, Vector2 anchor);

public class Menu : DrawableGameComponent
{
    // button grid in texture
    private static float BUTTON_GRID_HORIZONTAL_ANCHOR = 412;
    private static float BUTTON_GRID_VERTICAL_ANCHOR = 0;
    private static float BUTTON_GRID_HORIZONTAL_OFFSET = 190;
    private static float BUTTON_GRID_VERTICAL_OFFSET = 56;

    private static float BUTTON_GRID_INACTIVE_COL = 0;
    private static float BUTTON_GRID_ACTIVE_COL = 1;

    private static Vector2 BUTTON_SIZE = new Vector2(176, 55);
    private static float BUTTON_GAP = 2f;

    private static int MAIN_START_ROW = 0;
    private static int MAIN_SETTINGS_ROW = 1;
    private static int MAIN_QUIT_ROW = 2;
    private static int QUIT_YES_ROW = 3;
    private static int QUIT_NO_ROW = 4;
    private static int PLAYERS_2_ROW = 5;
    private static int PLAYERS_3_ROW = 6;
    private static int PLAYERS_4_ROW = 7;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private MenuState _state = MenuState.TITLE;

    private string _titlePath;
    private Texture2D _title;

    private string _menuItemsPath;
    private Texture2D _menuItems;

    private MenuGroup _startMenu;
    private MenuGroup _quitMenu;
    private MenuGroup _playersMenu;

    public Menu(Game game) : base(game)
    {
        _game = (GameMain)game;

        _titlePath = "Menu/title";
        _menuItemsPath = "Menu/items";

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.MENU_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.MENU_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        _title = GameMain.Content.Load<Texture2D>(_titlePath);
        _menuItems = GameMain.Content.Load<Texture2D>(_menuItemsPath);

        LoadStartMenuGroup();
        LoadQuitMenuGroup();
        LoadPlayersMenuGroup();
    }

    private void LoadStartMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (MAIN_START_ROW, MenuState.MAIN_START),
            (MAIN_SETTINGS_ROW, MenuState.MAIN_SETTINGS),
            (MAIN_QUIT_ROW, MenuState.MAIN_QUIT)
        };
        _startMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count));
    }

    private void LoadQuitMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (QUIT_YES_ROW, MenuState.QUIT_YES),
            (QUIT_NO_ROW, MenuState.QUIT_NO),
        };
        _quitMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count));
    }

    private void LoadPlayersMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (PLAYERS_2_ROW, MenuState.PLAYERS_2),
            (PLAYERS_3_ROW, MenuState.PLAYERS_3),
            (PLAYERS_4_ROW, MenuState.PLAYERS_4),
        };
        _playersMenu = new MenuGroup(buttons, CalculateAnchorOfMenuGroup(buttons.Count));
    }

    private Vector2 CalculateAnchorOfMenuGroup(int numberOfButons)
    {
        Vector2 screenCenter = new Vector2(
            GameMain.GetBackBufferWidth() * 0.5f,
            GameMain.GetBackBufferHeight() * 0.5f
        );
        float totalHeight = numberOfButons * BUTTON_SIZE.Y + (numberOfButons - 1) * BUTTON_GAP;
        Vector2 anchor = screenCenter - BUTTON_SIZE * 0.5f;
        anchor.Y -= totalHeight * 0.5f;

        return anchor;
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
                    GameMain.StartMatch(2);
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
                    GameMain.StartMatch(3);
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
                    GameMain.StartMatch(4);
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

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        switch (_state)
        {
            case MenuState.TITLE:
                DrawFullScreen(GameMain.SpriteBatch, _title);
                break;
            case MenuState.MAIN_START:
            case MenuState.MAIN_SETTINGS:
            case MenuState.MAIN_QUIT:
                DrawMenuGroup(GameMain.SpriteBatch, _startMenu);
                break;
            case MenuState.PLAYERS_2:
            case MenuState.PLAYERS_3:
            case MenuState.PLAYERS_4:
                DrawMenuGroup(GameMain.SpriteBatch, _playersMenu);
                break;
            case MenuState.SETTINGS:
                break;
            case MenuState.QUIT_YES:
            case MenuState.QUIT_NO:
                DrawMenuGroup(GameMain.SpriteBatch, _quitMenu);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), $"Unexpected menu state: {_state}");
        }

        GameMain.SpriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawFullScreen(SpriteBatch spriteBatch, Texture2D texture)
    {
        Vector2 scale = new Vector2(
                    GameMain.GetBackBufferWidth() / (float)texture.Width,
                    GameMain.GetBackBufferHeight() / (float)texture.Height
                );
        GameMain.SpriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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

            Vector2 startPosition = group.anchor;
            startPosition.Y += (BUTTON_SIZE.Y + BUTTON_GAP) * i;
            GameMain.SpriteBatch.Draw(
                _menuItems,
                position: startPosition,
                sourceRectangle: area,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
            0f);
        }
    }
}