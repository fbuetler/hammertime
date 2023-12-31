using System;
using hammertime;
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
#if DEBUG
        DrawShadowedString(
            _font, $"Map: {GameMain.Match.MapIndex}",
            new Vector2(leftAlignedOffset, topAlignedOffset),
            Color.White
        );
        DrawShadowedString(
            _font, $"Players alive: {GameMain.Match.Map.PlayersAlive.Count}",
            new Vector2(leftAlignedOffset, topAlignedOffset + nextLineOffset),
            Color.White
        );
#endif

        float screenWidth = GameMain.GetScreenWidth();
        DrawShadowedString(
            _font, "Scores:",
            new Vector2(screenWidth - rightAlignedOffset, topAlignedOffset),
            Color.White
        );
        DrawShadowedString(
            _font, $"Goal: {GameMain.Match.NumberOfRounds}",
            new Vector2(screenWidth - rightAlignedOffset, topAlignedOffset + nextLineOffset),
            Color.White
        );
        for (int i = 0; i < GameMain.Match.Scores.Length; i++)
        {
            int score = GameMain.Match.Scores[i];
            DrawShadowedString(
                _font,
                $"P{i + 1}: {score}",
                new Vector2(screenWidth - rightAlignedOffset, topAlignedOffset + nextLineOffset * (i + 2)),
                Color.White
            );
        }

        float screenHeight = GameMain.GetScreenHeight();
        if (GameMain.Match.MatchFinished || GameMain.Match.Map.Paused)
        {
            string text;
            if (GameMain.Match.MatchFinished)
            {
                int winnerId = Array.IndexOf(GameMain.Match.Scores, GameMain.Match.NumberOfRounds);
                text = $"WINNER: P{winnerId + 1}";
            }
            else if (GameMain.Match.Map.Paused)
            {
                text = "PAUSED";
            }
            else
            {
                throw new ArgumentOutOfRangeException("unkown condition");
            }

            Vector2 textSize = _font.MeasureString(text);
            DrawShadowedString(
                _font, text,
                new Vector2(
                    screenWidth / 2 - textSize.X / 2,
                    topAlignedOffset + nextLineOffset
                ),
                Color.White
            );
        }

#if DEBUG
        switch (GameMain.Match.ScoreState)
        {
            case ScoreState.None:
                break;
            case ScoreState.Winner:
                DrawShadowedString(
                    _font,
                    $"Winner: Player {GameMain.Match.RoundWinnerId + 1}",
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
#endif
    }

    private void DrawShadowedString(SpriteFont _font, string value, Vector2 position, Color color)
    {
        GameMain.SpriteBatch.DrawString(_font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        GameMain.SpriteBatch.DrawString(_font, value, position, color);
    }
}