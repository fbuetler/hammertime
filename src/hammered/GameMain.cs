using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace hammered;

public class GameMain : Game
{

    private GraphicsDeviceManager _graphics;

    private Map _map;

    // We store our input states so that we only poll once per frame, 
    // then we use the same input state wherever needed
    private GamePadState _gamePadState;
    private KeyboardState _keyboardState;

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    public int GetBackBufferWidth()
    {
        return _graphics.PreferredBackBufferWidth;
    }

    public int GetBackBufferHeight()
    {
        return _graphics.PreferredBackBufferHeight;
    }

    protected override void Initialize()
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0); // set frame rate to 144 fps
        IsFixedTimeStep = true; // decouple draw from update

        base.Initialize();

        // set window size to 720p
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        LoadMap();
    }

    private void LoadMap()
    {
        // Unloads the content for the current map before loading the next one.
        if (_map != null)
            _map.Dispose();

        _map = new Map(this, Services);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        HandleInput(gameTime);

        _map.Update(gameTime, _keyboardState, _gamePadState);

        base.Update(gameTime);
    }

    private void HandleInput(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();
        _gamePadState = GamePad.GetState(PlayerIndex.One); // TODO (fbuetler) more players

        if (_keyboardState.IsKeyDown(Keys.Escape) || _gamePadState.IsButtonDown(Buttons.Back))
            this.Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _map.Draw(gameTime);

        base.Draw(gameTime);
    }

}
