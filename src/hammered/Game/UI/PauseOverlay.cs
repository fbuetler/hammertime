using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

enum PauseMenuState
{
    RESTART,
    QUIT,
}

record PauseMenuComponent(string path, Texture2D texture);

public class PauseOverlay : DrawableGameComponent
{
    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private PauseMenuState _state = PauseMenuState.RESTART;

    private Dictionary<PauseMenuState, PauseMenuComponent> _menus;

    private const string texturePath = "Overlays/Pause/pause";

    public PauseOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        _menus = new Dictionary<PauseMenuState, PauseMenuComponent>();
        _menus[PauseMenuState.RESTART] = new PauseMenuComponent("Overlays/Pause/press_restart", null);
        _menus[PauseMenuState.QUIT] = new PauseMenuComponent("Overlays/Pause/press_quit", null);

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.PAUSE_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.PAUSE_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        foreach (PauseMenuState state in Enum.GetValues(typeof(PauseMenuState)))
        {
            string path = _menus[state].path;
            var texture = GameMain.Content.Load<Texture2D>(path);
            _menus[state] = new PauseMenuComponent(path, texture);
        }

        GameMain.AudioManager.LoadSoundEffect(Menu.InteractButtonPressSoundEffect);
        GameMain.AudioManager.LoadSoundEffect(Menu.AlternativeButtonPressSoundEffect);
    }

    public override void Update(GameTime gameTime)
    {
        Visible = GameMain.Match.Map.Paused;
        if (!GameMain.Match.Map.Paused)
        {
            return;
        }

        PauseMenuState prev = _state;
        switch (_state)
        {
            case PauseMenuState.RESTART:
                if (Controls.FocusPrev.Pressed())
                    _state = PauseMenuState.QUIT;
                else if (Controls.FocusNext.Pressed())
                    _state = PauseMenuState.QUIT;
                else if (Controls.Interact.Pressed())
                {
                    GameMain.Match.LoadMap();
                    _state = PauseMenuState.RESTART;
                }
                else if (Controls.Back.Pressed())
                {
                    GameMain.Match.Map.TogglePause();
                    _state = PauseMenuState.RESTART;
                }
                break;
            case PauseMenuState.QUIT:
                if (Controls.FocusPrev.Pressed())
                    _state = PauseMenuState.RESTART;
                else if (Controls.FocusNext.Pressed())
                    _state = PauseMenuState.RESTART;
                else if (Controls.Interact.Pressed())
                {
                    GameMain.EndMatch();
                    _state = PauseMenuState.RESTART;
                }
                else if (Controls.Back.Pressed())
                {
                    GameMain.Match.Map.TogglePause();
                    _state = PauseMenuState.RESTART;
                }
                break;
        }

        if (_state != prev)
        {
            if (Controls.Interact.Pressed())
            {
                GameMain.AudioManager.PlaySoundEffect(Menu.InteractButtonPressSoundEffect);
            }
            else
            {
                GameMain.AudioManager.PlaySoundEffect(Menu.AlternativeButtonPressSoundEffect);
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);
        DrawFullScreen(GameMain.SpriteBatch, _menus[_state].texture);
        GameMain.SpriteBatch.End();

        base.Draw(gameTime);
    }

    protected void DrawFullScreen(SpriteBatch spriteBatch, Texture2D texture)
    {
        Vector2 scale = new Vector2(
                    GameMain.GetScreenWidth() / (float)texture.Width,
                    GameMain.GetScreenHeight() / (float)texture.Height
                );
        spriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}