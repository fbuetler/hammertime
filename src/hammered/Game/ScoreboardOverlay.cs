using System;
using hammered;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ScoreboardOverlay : DrawableGameComponent
{
    private static Rectangle PLAYER_BUBBLE = new Rectangle(
        0, 0,
        37, 42
    );
    private static Vector2 PLAYER_BUBBLE_OFFSET = new Vector2(56, 0);

    private static Rectangle SCORE_BUBBLE = new Rectangle(
        0, 75,
        31, 34
    );
    private static Vector2 SCORE_BUBBLE_OFFSET = new Vector2(37, 0);

    private static Rectangle SCOREBAR = new Rectangle(
        0, 113,
        381, 58
    );

    private static float MARGIN = 5f;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private Texture2D _scoreItems;

    private Rectangle _scoreBar;
    private Rectangle[] _playerBubbles;
    private Rectangle[] _scoreBubbles;
    private Rectangle _emptyScoreBubble;

    public ScoreboardOverlay(Game game) : base(game)
    {
        _game = (GameMain)game;

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.HUD_UPDATE_ORDER;
        Visible = false;
        DrawOrder = GameMain.OVERLAY_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        _scoreItems = GameMain.Content.Load<Texture2D>("Menu/items");

        LoadScoreBar();
        LoadPlayerBubbles();
        LoadScoreBubbles();
    }

    private void LoadScoreBar()
    {
        _scoreBar = SCOREBAR; ;
    }

    private void LoadPlayerBubbles()
    {
        _playerBubbles = new Rectangle[Match.MaxNumberOfPlayers];
        for (int i = 0; i < _playerBubbles.Length; i++)
        {
            _playerBubbles[i] = PLAYER_BUBBLE;
            _playerBubbles[i].Offset(i * PLAYER_BUBBLE_OFFSET);
        }
    }

    private void LoadScoreBubbles()
    {
        _scoreBubbles = new Rectangle[Match.MaxNumberOfPlayers];
        for (int i = 0; i < _scoreBubbles.Length; i++)
        {
            _scoreBubbles[i] = SCORE_BUBBLE;
            _scoreBubbles[i].Offset(i * SCORE_BUBBLE_OFFSET);
        }
        _emptyScoreBubble = SCORE_BUBBLE;
        _emptyScoreBubble.Offset(Match.MaxNumberOfPlayers * SCORE_BUBBLE_OFFSET);
    }

    public override void Update(GameTime gameTime)
    {
        switch (GameMain.Match.ScoreState)
        {
            case ScoreState.None:
                Visible = false;
                break;
            case ScoreState.Winner:
            case ScoreState.Draw:
                Visible = true;
                break;
            default:
                throw new NotSupportedException(String.Format("Scorestate type '{0}' is not supported", GameMain.Match.ScoreState));
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);

        DrawScoreBars(GameMain.SpriteBatch, GameMain.Match.NumberOfPlayers);

        GameMain.SpriteBatch.End();
    }

    private void DrawScoreBars(SpriteBatch spriteBatch, int numberOfPlayers)
    {
        Vector2 anchor = CalculateAnchor(numberOfPlayers);
        for (int i = 0; i < numberOfPlayers; i++)
        {
            Vector2 scoreBarPosition = anchor - SCOREBAR.Size.ToVector2() * 0.5f;
            scoreBarPosition.Y += (SCOREBAR.Height + MARGIN) * i;
            spriteBatch.Draw(
                _scoreItems,
                position: scoreBarPosition,
                sourceRectangle: SCOREBAR,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );

            Vector2 playerBubblePosition = scoreBarPosition;
            playerBubblePosition.Y += (SCOREBAR.Height - PLAYER_BUBBLE.Height) * 0.5f;
            playerBubblePosition.X -= PLAYER_BUBBLE.Width + MARGIN;
            spriteBatch.Draw(
                _scoreItems,
                position: playerBubblePosition,
                sourceRectangle: _playerBubbles[i],
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );


            Vector2 scoreBubblePosition = scoreBarPosition;
            scoreBubblePosition.Y += (SCOREBAR.Height - SCORE_BUBBLE.Height) * 0.5f;
            float bubbleOffset = SCOREBAR.Width / (Match.MaxPoints + 1);
            scoreBubblePosition.X += bubbleOffset * 0.5f;
            for (int j = 0; j < Match.MaxPoints; j++)
            {
                Rectangle bubble;
                if (GameMain.Match.Scores[i] > j)
                {
                    bubble = _scoreBubbles[i];
                }
                else
                {
                    bubble = _emptyScoreBubble;
                }

                spriteBatch.Draw(
                    _scoreItems,
                    position: scoreBubblePosition,
                    sourceRectangle: bubble,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 1f,
                    effects: SpriteEffects.None,
                    layerDepth: 0f
                );

                scoreBubblePosition.X += bubbleOffset;
            }
        }
    }

    private Vector2 CalculateAnchor(int numberOfPlayers)
    {
        Vector2 screenCenter = new Vector2(
            GameMain.GetBackBufferWidth() * 0.5f,
            GameMain.GetBackBufferHeight() * 0.5f
        );
        // TODO (fbuetler) distinguish even (center is between buttons) and odd number of buttons (center is on a button)
        float totalHeight = numberOfPlayers * SCORE_BUBBLE.Height + (numberOfPlayers - 1) * MARGIN;
        Vector2 anchor = screenCenter;
        anchor.Y -= totalHeight * 0.5f;

        return anchor;
    }
}