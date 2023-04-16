﻿using System;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public class GameMain : Game
{
    // update order from high to low (default is 0)
    public const int MENU_UPDATE_ORDER = 0;
    public const int MATCH_UPDATE_ORDER = 0;
    public const int MAP_UPDATE_ORDER = 0;
    public const int HUD_UPDATE_ORDER = 0;
    public const int TILE_UPDATE_ORDER = 1;
    public const int HAMMER_UPDATE_ORDER = 2;
    public const int PLAYER_UPDATE_ORDER = 3;

    // draw order from high to low (default is 0)
    public const int MENU_DRAW_ORDER = 0; // clears screen
    public const int MAP_DRAW_ORDER = 0; // clears screen
    public const int MATCH_DRAW_ORDER = 1;
    public const int HUD_DRAW_ORDER = 1;
    public const int HAMMER_DRAW_ORDER = 2;
    public const int PLAYER_DRAW_ORDER = 3;
    public const int TILE_DRAW_ORDER = 4;

    // drawing
    private GraphicsDeviceManager _graphics;

    public SpriteBatch SpriteBatch { get => _spriteBatch; }
    private SpriteBatch _spriteBatch;

    public DebugDraw DebugDraw { get => _debugDraw; }
    private DebugDraw _debugDraw;

    private Menu _menu;

    public Match Match { get => _match; }
    private Match _match;

    public AudioManager AudioManager { get => _audio; }
    private AudioManager _audio;


    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    public int GetBackBufferWidth()
    {
#if DEBUG
        return 1280;
#else
        return 1920;
#endif
    }

    public int GetBackBufferHeight()
    {
#if DEBUG
        return 720;
#else
        return 1080;
#endif
    }

    protected override void Initialize()
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0); // set frame rate to 144 fps
        IsFixedTimeStep = true; // decouple draw from update

        _menu = new Menu(this);
        Components.Add(_menu);

        base.Initialize();

        _graphics.PreferredBackBufferWidth = GetBackBufferWidth();
        _graphics.PreferredBackBufferHeight = GetBackBufferHeight(); ;

        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();

        _debugDraw = new DebugDraw(GraphicsDevice);
        _audio = new AudioManager(this);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        InputHelper.Setup(this);
    }
    public void StartMatch(int NumberOfPlayers)
    {
        _match = new Match(this, NumberOfPlayers);
        Components.Add(_match);

        _menu.Visible = false;
        Components.Remove(_menu);
    }

    protected override void Update(GameTime gameTime)
    {
        InputHelper.UpdateSetup();
        HandleInput();
        base.Update(gameTime);
        InputHelper.UpdateCleanup();
    }

    private void HandleInput()
    {
        if (Controls.Back.Pressed())
        {
            Exit();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}
