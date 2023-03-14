using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace hammered;

public class GameMain : Game
{
    // drawing
    private GraphicsDeviceManager _graphics;

    // game state
    private int _mapIndex = -1;
    private Map _map;
    private bool _wasContinuePressed;

    // store input states so that they are only polled once per frame, 
    // then the same input state is used wherever needed
    private GamePadState _gamePadState;
    private KeyboardState _keyboardState;

    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 1;

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
        LoadNextMap();
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

        bool continuePressed = _keyboardState.IsKeyDown(Keys.Space) || _gamePadState.IsButtonDown(Buttons.Start);

        // Perform the appropriate action to advance the game and
        // to get the player back to playing.
        if (!_wasContinuePressed && continuePressed)
        {
            ReloadCurrentMap();
        }

        _wasContinuePressed = continuePressed;
    }

    private void LoadNextMap()
    {
        // unloads the content for the current map before loading the next one
        if (_map != null)
            _map.Dispose();

        // TODO (fbuetler) allow multiple maps
        _mapIndex = (_mapIndex + 1) % numberOfMaps;
        // string mapPath = string.Format("Content/Levels/{0}.txt", _mapIndex);
        // using (Stream fileStream = TitleContainer.OpenStream(mapPath))
        //     _map = new Map(this, fileStream, Services);

        _map = new Map(this, Services);
    }

    private void ReloadCurrentMap()
    {
        _mapIndex--;
        LoadNextMap();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _map.Draw(gameTime);

        base.Draw(gameTime);
    }

}
