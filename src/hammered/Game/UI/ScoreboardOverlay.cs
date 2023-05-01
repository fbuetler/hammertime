using System;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ScoreboardOverlay : DrawableGameComponent
{
    private static Rectangle SCORE = new Rectangle(
        0, 0,
        237, 237
    );

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private Texture2D[] _scoreTextures;
    private Rectangle[] _scoreSourceRectangles;

    private const string scoreTexturePrefix = "Overlays/Scoreboard";

    public ScoreboardOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        _scoreTextures = new Texture2D[Match.MaxNumberOfPlayers];
        _scoreSourceRectangles = new Rectangle[Match.MaxNumberOfRounds + 1];

        // make update and draw called by monogame
        Enabled = false;
        Visible = true;
        DrawOrder = GameMain.OVERLAY_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        for (int i = 0; i < Match.MaxNumberOfPlayers; i++)
        {
            _scoreTextures[i] = GameMain.Content.Load<Texture2D>($"{scoreTexturePrefix}/{i}");
        }
        LoadScores();
    }

    private void LoadScores()
    {
        for (int j = 0; j < _scoreSourceRectangles.Length - 2; j++)
        {
            _scoreSourceRectangles[j] = new Rectangle(
                j * SCORE.Width,
                0,
                SCORE.Width,
                SCORE.Height
            );
        }
        // TODO (fbuetler) fix special cases
        _scoreSourceRectangles[9] = new Rectangle(
            2130,
            0,
            200,
            SCORE.Height
        );
        _scoreSourceRectangles[10] = new Rectangle(
            2330,
            0,
            250,
            SCORE.Height
        );
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
                _scoreTextures[playerId],
                position: scorePosition,
                sourceRectangle: _scoreSourceRectangles[score],
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