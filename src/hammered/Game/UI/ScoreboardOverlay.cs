using System;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ScoreboardOverlay : DrawableGameComponent
{
    private static Rectangle SCORE = new Rectangle(
        0, 0,
        150, 300
    );

    private static Point SCORE_ROW_OFFSET = new Point(0, 400);

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private Texture2D _scoreTexture;

    private Rectangle[,] _scoreSourceRectangles;

    public ScoreboardOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        _scoreSourceRectangles = new Rectangle[Match.MaxNumberOfPlayers, Match.MaxNumberOfRounds];

        // make update and draw called by monogame
        Enabled = false;
        Visible = true;
        DrawOrder = GameMain.OVERLAY_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        _scoreTexture = GameMain.Content.Load<Texture2D>("Overlays/Scoreboard/scores");
        LoadScores();
    }

    private void LoadScores()
    {
        for (int i = 0; i < Match.MaxNumberOfPlayers; i++)
        {
            for (int j = 0; j < Match.MaxNumberOfRounds; j++)
            {
                _scoreSourceRectangles[i, j] = new Rectangle(
                    j * SCORE.Width,
                    i * SCORE_ROW_OFFSET.Y,
                    SCORE.Width,
                    SCORE.Height
                );
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        DrawScores(GameMain.SpriteBatch, GameMain.Match.Scores);

        GameMain.SpriteBatch.End();
    }

    private void DrawScores(SpriteBatch spriteBatch, int[] scores)
    {
        Vector2 scoresSize = scores.Length * SCORE.Size.ToVector2();
        // centered
        Vector2 anchor = new Vector2(
            0.5f * (GameMain.GetScreenWidth() - scoresSize.X),
            0
        );

        for (int playerId = 0; playerId < scores.Length; playerId++)
        {
            int score = scores[playerId];
            Vector2 scorePosition = new Vector2(
                anchor.X + playerId * SCORE.Width,
                anchor.Y
            );
            spriteBatch.Draw(
                _scoreTexture,
                position: scorePosition,
                sourceRectangle: _scoreSourceRectangles[playerId, score],
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }
}