using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace hammered;

public enum ScoreState
{
    None = 0,
    Winner = 1,
    Draw = 2,
}

public class GameMain : Game
{
    // drawing
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public DebugDraw DebugDraw
    {
        get { return _debugDraw; }
    }
    private DebugDraw _debugDraw;

    SpriteFont font;

    // game state
    private int _mapIndex = -1;
    private Map _map;
    private bool _wasReloadPressed;
    private bool _wasNextPressed;
    private ScoreState _scoreState;
    private int _winnerID;

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
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        font = Content.Load<SpriteFont>("Fonts/font");

        LoadNextMap();
    }

    protected override void Update(GameTime gameTime)
    {
        HandleInput(gameTime);

        _map.Update(gameTime, _keyboardState, _gamePadStates);

        base.Update(gameTime);
    }

    private void HandleInput(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        _gamePadStates = new GamePadState[4];
        for (int i = 0; i < NumberOfPlayers; i++)
        {
            _gamePadStates[i] = GamePad.GetState(i);
        }

        // TODO (fbuetler) proper input handling instead of just taking the input of the first player
        if (_keyboardState.IsKeyDown(Keys.Escape) || _gamePadStates[0].IsButtonDown(Buttons.Back))
            this.Exit();

        bool reloadPressed = _keyboardState.IsKeyDown(Keys.R) || _gamePadStates[0].IsButtonDown(Buttons.Start);
        if (!_wasReloadPressed && reloadPressed)
        {
            ReloadCurrentMap();
        }
        _wasReloadPressed = reloadPressed;

        bool nextPressed = _keyboardState.IsKeyDown(Keys.N) || _gamePadStates[0].IsButtonDown(Buttons.Y);
        if (!_wasNextPressed && nextPressed)
        {
            LoadNextMap();
        }
        _wasNextPressed = nextPressed;
    }

    private void LoadNextMap()
    {
        // unloads the content for the current map before loading the next one
        if (_map != null)
            _map.Dispose();

        _scoreState = ScoreState.None;
        _winnerID = -1;

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

        _map.Draw(gameTime);

        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        _spriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        // TODO (fbuetler) create start screen
        DrawHud();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawHud()
    {
        DrawShadowedString(font, "Map: " + _mapIndex, new Vector2(10, 10), Color.White);

        List<int> playersAlive = new List<int>();
        foreach (Player p in _map.Players)
        {
            if (p.IsAlive)
            {
                playersAlive.Add(p.ID);
            }
        }

        DrawShadowedString(font, "Players alive: " + playersAlive.Count, new Vector2(10, 60), Color.White);

        // TODO (fbuetle) draw overlay instead of strings
        switch (_scoreState)
        {
            case ScoreState.None:
                if (playersAlive.Count == 1)
                {
                    _scoreState = ScoreState.Winner;
                    _winnerID = playersAlive[0];
                }
                else if (playersAlive.Count == 0)
                {
                    _scoreState = ScoreState.Draw;
                }
                break;
            case ScoreState.Winner:
                DrawShadowedString(font, "Winner: Player " + (_winnerID + 1), new Vector2(10, 110), Color.White);
                break;
            case ScoreState.Draw:
                DrawShadowedString(font, "Draw", new Vector2(10, 120), Color.White);
                break;
            default:
                throw new NotSupportedException(String.Format("Scorestate type '{0}' is not supported", _scoreState));
        }
    }

    private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
    {
        _spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        _spriteBatch.DrawString(font, value, position, color);
    }
}
