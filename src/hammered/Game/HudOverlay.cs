using System;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class HudOverlay : DrawableGameComponent
{

    public GameMain GameMain { get => _game; }
    private GameMain _game;
    private SpriteFont _font;

    private const float topAlignedOffset = 10;
    private const float leftAlignedOffset = 10;
    private const float rightAlignedOffset = 150;
    private const float nextLineOffset = 50;

    public HudOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.HUD_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.OVERLAY_DRAW_ORDER;
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

        DrawHud();

        GameMain.SpriteBatch.End();
    }

    private void DrawHud()
    {
        DrawShadowedString(
            _font, $"Map: {GameMain.Match.MapIndex}",
            new Vector2(leftAlignedOffset, topAlignedOffset),
            Color.White
        );
        DrawShadowedString(
            _font, $"Players alive: {GameMain.Match.PlayersAlive.Count}",
            new Vector2(leftAlignedOffset, topAlignedOffset + nextLineOffset),
            Color.White
        );

        float screenWidth = GameMain.GetBackBufferWidth();
        DrawShadowedString(
            _font, "Scores:",
            new Vector2(screenWidth - rightAlignedOffset, topAlignedOffset),
            Color.White
        );
        for (int i = 0; i < GameMain.Match.Scores.Length; i++)
        {
            int score = GameMain.Match.Scores[i];
            DrawShadowedString(
                _font,
                $"P{i + 1}: {score}",
                new Vector2(screenWidth - rightAlignedOffset, topAlignedOffset + nextLineOffset * (i + 1)),
                Color.White
            );
        }

        float screenHeight = GameMain.GetBackBufferHeight();
        if (GameMain.Match.Paused)
        {
            string paused = "PAUSED";
            Vector2 textSize = _font.MeasureString(paused);
            DrawShadowedString(
                _font, paused,
                new Vector2(
                    screenWidth / 2 - textSize.X / 2,
                    screenHeight / 2 - textSize.Y / 2
                ),
                Color.White
            );
        }

        switch (GameMain.Match.ScoreState)
        {
            case ScoreState.None:
                // do nothing - game is still going on
                break;
            case ScoreState.Winner:
                DrawShadowedString(
                    _font,
                    $"Winner: Player {GameMain.Match.WinnerId + 1}",
                    new Vector2(leftAlignedOffset, topAlignedOffset + 2 * nextLineOffset),
                    Color.White
                );
                break;
            case ScoreState.Draw:
                DrawShadowedString(
                    _font,
                    "Draw",
                    new Vector2(leftAlignedOffset, topAlignedOffset + 2 * nextLineOffset),
                    Color.White
                );
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