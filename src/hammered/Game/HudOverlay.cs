using System;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class HudOverlay : DrawableGameComponent
{

    public GameMain GameMain { get => _game; }
    private GameMain _game;
    private SpriteFont _font;

    public HudOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.HUD_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.HUD_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/arial");
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        // TODO (fbuetler) create start screen
        DrawHud();

        GameMain.SpriteBatch.End();
    }

    private void DrawHud()
    {
        DrawShadowedString(_font, "Map: " + GameMain.Match.MapIndex, new Vector2(10, 10), Color.White);
        DrawShadowedString(_font, "Players alive: " + GameMain.Match.PlayersAlive.Count, new Vector2(10, 60), Color.White);

        // TODO (fbuetle) draw overlay instead of strings
        switch (GameMain.Match.ScoreState)
        {
            case ScoreState.None:
                // do nothing - game is still going on
                break;
            case ScoreState.Winner:
                DrawShadowedString(_font, "Winner: Player " + (GameMain.Match.WinnerId + 1), new Vector2(10, 110), Color.White);
                break;
            case ScoreState.Draw:
                DrawShadowedString(_font, "Draw", new Vector2(10, 120), Color.White);
                break;
            default:
                throw new NotSupportedException(String.Format("Scorestate type '{0}' is not supported", GameMain.Match.ScoreState));
        }
    }

    private void DrawShadowedString(SpriteFont _font, string value, Vector2 position, Color color)
    {
        GameMain.SpriteBatch.DrawString(_font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        GameMain.SpriteBatch.DrawString(_font, value, position, color);
    }
}