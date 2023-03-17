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
    public DebugDraw DebugDraw
    {
        get { return _debugDraw; }
    }
    private DebugDraw _debugDraw;

    // game state
    private int _mapIndex = -1;
    private Map _map;
    private bool _wasReloadPressed;
    private bool _wasContinuePressed;

    // store input states so that they are only polled once per frame, 
    // then the same input state is used wherever needed
    private GamePadState[] _gamePadStates;
    private KeyboardState _keyboardState;

    public const int NumberOfPlayers = 2;
    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 2;

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

        _debugDraw = new DebugDraw(GraphicsDevice);
    }

    protected override void LoadContent()
    {
        LoadNextMap();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        HandleInput(gameTime);

        _map.Update(gameTime, _keyboardState, _gamePadStates);

        base.Update(gameTime);
    }

    private void HandleInput(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        _gamePadStates = new GamePadState[NumberOfPlayers];
        for (int i = 0; i < NumberOfPlayers; i++)
        {
            GamePadCapabilities gamePadCapabilities = GamePad.GetCapabilities(i);
            if (gamePadCapabilities.IsConnected)
            {
                _gamePadStates[i] = GamePad.GetState(i);
            }
        }

        if (_keyboardState.IsKeyDown(Keys.Escape) || _gamePadStates[0].IsButtonDown(Buttons.Back))
            this.Exit();

        bool reloadPressed = _keyboardState.IsKeyDown(Keys.Space) || _gamePadStates[0].IsButtonDown(Buttons.Start);
        if (!_wasReloadPressed && reloadPressed)
        {
            ReloadCurrentMap();
        }
        _wasReloadPressed = reloadPressed;

        bool continuePressed = _keyboardState.IsKeyDown(Keys.Enter) || _gamePadStates[0].IsButtonDown(Buttons.Y);
        if (!_wasContinuePressed && continuePressed)
        {
            LoadNextMap();
        }
        _wasContinuePressed = continuePressed;
    }

    private void LoadNextMap()
    {
        // unloads the content for the current map before loading the next one
        if (_map != null)
            _map.Dispose();

        _mapIndex = (_mapIndex + 1) % numberOfMaps;
        string mapPath = string.Format("Content/Maps/{0}.txt", _mapIndex);
        using (Stream fileStream = TitleContainer.OpenStream(mapPath))
            _map = new Map(this, Services, fileStream);
    }

    private void ReloadCurrentMap()
    {
        _mapIndex--;
        LoadNextMap();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO (fbuetler) draw map index on the screen

        _map.Draw(gameTime);

        base.Draw(gameTime);
    }

}
