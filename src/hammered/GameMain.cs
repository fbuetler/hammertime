﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    public SpriteBatch SpriteBatch { get => _spriteBatch; }
    private SpriteBatch _spriteBatch;

    public DebugDraw DebugDraw
    {
        get { return _debugDraw; }
    }
    private DebugDraw _debugDraw;

    private Map _map;
    public Map Map { get => _map; }

    // game state
    public int MapIndex { get => _mapIndex; }
    private int _mapIndex = 0;

    private bool _wasReloadPressed;
    private bool _wasNextPressed;
    private ScoreState _scoreState;
    public ScoreState ScoreState { get => _scoreState; }
    private List<int> _playersAlive;
    public List<int> PlayersAlive { get => _playersAlive; }

    public int? WinnerId { get => _winnerId; }
    private int? _winnerId = null;

    // store input states so that they are only polled once per frame, 
    // then the same input state is used wherever needed
    private GamePadState[] _gamePadStates;
    private KeyboardState _keyboardState;

    private int _numberOfPlayers = 4;
    public int NumberOfPlayers
    {
        get => _numberOfPlayers;
    }

    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 3;

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

        InitializeComponents();

        base.Initialize();

        // set window size to 720p
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();

        _debugDraw = new DebugDraw(GraphicsDevice);
    }

    private void InitializeComponents()
    {
        // initialize game objects
        LoadMap(_mapIndex);

        // initialize game overlay
        Components.Add(new HudOverlay(this));
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        HandleInput(gameTime);
        Map.Update(gameTime, _keyboardState, _gamePadStates);
        UpdateGameState();
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
        {
            this.Exit();
        }

        bool reloadPressed = _keyboardState.IsKeyDown(Keys.R) || _gamePadStates[0].IsButtonDown(Buttons.Start);
        if (!_wasReloadPressed && reloadPressed)
        {
            InitializeComponents();
        }
        _wasReloadPressed = reloadPressed;

        bool nextPressed = _keyboardState.IsKeyDown(Keys.N) || _gamePadStates[0].IsButtonDown(Buttons.Y);
        if (!_wasNextPressed && nextPressed)
        {
            _mapIndex = (_mapIndex + 1) % numberOfMaps;
            InitializeComponents();
        }
        _wasNextPressed = nextPressed;
    }

    private void UpdateGameState()
    {
        foreach (Player p in _map.Players.Values)
        {
            // TODO: (lmeinen) Wait with decreasing playsAlive until player hits ground below (could make for fun animation or items that allow one to come back from falling)
            if (p.State == PlayerState.DEAD || p.State == PlayerState.FALLING)
            {
                _playersAlive.Remove(p.PlayerId);
            }
        }

        if (_scoreState == ScoreState.None)
        {
            if (_playersAlive.Count == 1)
            {
                _scoreState = ScoreState.Winner;
                _winnerId = _playersAlive[0];
            }
            else if (_playersAlive.Count == 0)
            {
                _scoreState = ScoreState.Draw;
            }
        }
    }

    private void LoadMap(int i)
    {
        // unloads the content for the current map before loading the next one
        if (Map != null)
            Map.Dispose();

        _scoreState = ScoreState.None;

        string mapPath = string.Format("Content/Maps/{0}.txt", _mapIndex);
        using (Stream fileStream = TitleContainer.OpenStream(mapPath))
            _map = new Map(this, Services, fileStream);

        _playersAlive = _map.Players.Keys.ToList();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Map.Draw(gameTime);
        base.Draw(gameTime);
    }
}
