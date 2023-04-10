using System;
using Apos.Gui;
using FontStashSharp;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.TextureAtlases;

enum MenuState
{
    Main,
    Settings,
    Quit
};

public class Menu : DrawableGameComponent
{
    private GameMain _game;
    private IMGUI _ui;
    private MenuState _state = MenuState.Main;

    private float _slider = 50f;

    private TextureRegion2D _texture;

    public Menu(Game game) : base(game)
    {
        _game = (GameMain)game;
    }

    protected override void LoadContent()
    {
        FontSystem fontSystem = FontSystemFactory.Create(GraphicsDevice, 2048, 2048);
        fontSystem.AddFont(TitleContainer.OpenStream($"{_game.Content.RootDirectory}/Fonts/Arial.ttf"));

        GuiHelper.Setup(_game, fontSystem);
        _ui = new IMGUI();
        GuiHelper.CurrentIMGUI = _ui;

        var texture = _game.Content.Load<Texture2D>("Menu/icecream");
        _texture = new TextureRegion2D(texture, 0, 0, texture.Width, texture.Height);
    }

    public override void Update(GameTime gameTime)
    {
        GuiHelper.UpdateSetup(gameTime);
        _ui.UpdateAll(gameTime);

        MenuPanel.Push();
        switch (_state)
        {
            case MenuState.Main:
                SetupMainMenu(gameTime);
                break;
            case MenuState.Settings:
                SetupSettingsMenu(gameTime);
                break;
            default:
                SetupQuitMenu(gameTime);
                break;
        }
        MenuPanel.Pop();

        if (Default.Back.Pressed())
        {
            if (_state == MenuState.Main)
            {
                _state = MenuState.Quit;
            }
            else
            {
                _state = MenuState.Main;
            }
        }

        GuiHelper.UpdateCleanup();
        base.Update(gameTime);
    }

    private void SetupMainMenu(GameTime gameTime)
    {
        Button settingsButton = Button.Put("Settings");
        if (settingsButton.Clicked)
            _state = MenuState.Settings;

        Button quitButton = Button.Put("Quit");
        if (quitButton.Clicked)
            _state = MenuState.Quit;
    }

    private void SetupSettingsMenu(GameTime gameTime)
    {
        Label.Put("Settings");

        Slider slider = Slider.Put(ref _slider, 0f, 100f, 1f);
        Label.Put($"{Math.Round(_slider, 1)}");

        Icon.Put(_texture);

        Button backButton = Button.Put("Back");
        if (backButton.Clicked)
            _state = MenuState.Main;
    }

    private void SetupQuitMenu(GameTime gameTime)
    {
        Label.Put("Quit Menu");

        Button yesButton = Button.Put("Yes");
        if (yesButton.Clicked)
            _game.Exit();

        Button noButton = Button.Put("No");
        if (noButton.Clicked)
            _state = MenuState.Main;
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _ui.Draw(gameTime);

        base.Draw(gameTime);
    }
}