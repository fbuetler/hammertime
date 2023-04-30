using System;
using System.Collections.Generic;
using System.Linq;
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

    PLAYERS,
    PLAYERS_CONFIRMED,

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

    private static float BUTTON_GRID_ACTIVE_COL = 0;
    private static float BUTTON_GRID_INACTIVE_COL = 1;

    private static Vector2 BUTTON_SIZE = new Vector2(176, 55);
    private static float MARGIN = 2f;

    private static int MAIN_START_ROW = 0;
    private static int MAIN_SETTINGS_ROW = 1;
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

    private static readonly Vector2 nextLineOffset = new Vector2(0, 50);

    // sound effects
    private const string InteractButtonPressSoundEffect = "MenuAudio/ButtonPress0";
    private const string AlternativeButtonPressSoundEffect = "MenuAudio/ButtonPress1";

    public Menu(Game game) : base(game)
    {
        _game = (GameMain)game;

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

        LoadStartMenuGroup();
        LoadQuitMenuGroup();
        LoadPlayersMenuGroup();

        GameMain.AudioManager.LoadSoundEffect(InteractButtonPressSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(AlternativeButtonPressSoundEffect);
    }

    private void LoadStartMenuGroup()
    {
        var buttons = new List<(int, MenuState)>{
            (MAIN_START_ROW, MenuState.MAIN_START),
            (MAIN_SETTINGS_ROW, MenuState.MAIN_SETTINGS),
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
        MenuState prev = _state;
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
                    _state = MenuState.PLAYERS;
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;
            case MenuState.MAIN_SETTINGS:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_QUIT;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.SETTINGS;
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;
            case MenuState.MAIN_QUIT:
                if (Controls.FocusPrev.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
                else if (Controls.FocusNext.Pressed())
                    _state = MenuState.MAIN_START;
                else if (Controls.Interact.Pressed())
                    _state = MenuState.QUIT_YES;
                else if (Controls.Back.Pressed())
                    _state = MenuState.TITLE;
                break;

            // players
            case MenuState.PLAYERS:
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
                    else if (Controls.BackP(i).Pressed())
                    {
                        _state = MenuState.MAIN_START;
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
                        _state = MenuState.MAIN_START;
                        GameMain.StartMatch(_playersConnected);
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
                    _state = MenuState.MAIN_START;
                    GameMain.StartMatch(4);
                }
#endif

                break;

            // settings
            case MenuState.SETTINGS:
                if (Controls.Back.Pressed())
                    _state = MenuState.MAIN_SETTINGS;
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

        if (_state != prev)
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

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        Vector2 screenCenter = GameMain.GetScreenCenter();

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
            case MenuState.PLAYERS:
            case MenuState.PLAYERS_CONFIRMED:
                Vector2 anchor = screenCenter - new Vector2(0, GameMain.GetScreenHeight() / 6);
                DrawString(GameMain.SpriteBatch, _impactFont, "LOOKING FOR PLAYERS...", anchor);
                for (int i = 0; i < _playersConnected; i++)
                {
                    string s = $"P{i + 1}: ";
                    if (!_playersConfirmed[i])
                    {
                        s += "connected";
                    }
                    else
                    {
                        s += "confirmed";
                    }
                    DrawString(GameMain.SpriteBatch, _impactFont, s, anchor + (i + 1) * nextLineOffset);
                }

                DrawMenuGroup(GameMain.SpriteBatch, _playersMenu);

                break;
            case MenuState.SETTINGS:
                DrawString(GameMain.SpriteBatch, _impactFont, "Settings", screenCenter);
                break;
            case MenuState.QUIT_YES:
            case MenuState.QUIT_NO:
                DrawString(GameMain.SpriteBatch, _impactFont, "QUIT?", _quitMenu.anchor);
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