using System;
using System.Collections.Generic;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class HudOverlay : DrawableGameComponent
{
    private GameMain _game;
    private SpriteFont _font;

    public HudOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;
    }

    protected override void LoadContent()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/font");
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        _game.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        // TODO (fbuetler) create start screen
        DrawHud();

        _game.SpriteBatch.End();
    }

    private void DrawHud()
    {
        DrawShadowedString(_font, "Map: " + _game.MapIndex, new Vector2(10, 10), Color.White);
        DrawShadowedString(_font, "Players alive: " + _game.PlayersAlive.Count, new Vector2(10, 60), Color.White);

        // TODO (fbuetle) draw overlay instead of strings
        switch (_game.ScoreState)
        {
            case ScoreState.None:
                // do nothing - game is still going on
                break;
            case ScoreState.Winner:
                DrawShadowedString(_font, "Winner: Player " + (_game.WinnerId + 1), new Vector2(10, 110), Color.White);
                break;
            case ScoreState.Draw:
                DrawShadowedString(_font, "Draw", new Vector2(10, 120), Color.White);
                break;
            default:
                throw new NotSupportedException(String.Format("Scorestate type '{0}' is not supported", _game.ScoreState));
        }
    }

    private void DrawShadowedString(SpriteFont _font, string value, Vector2 position, Color color)
    {
        _game.SpriteBatch.DrawString(_font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        _game.SpriteBatch.DrawString(_font, value, position, color);
    }
}