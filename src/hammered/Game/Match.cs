using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace hammered;

public enum ScoreState
{
    None = 0,
    Winner = 1,
    Draw = 2,
}

public class Match : DrawableGameComponent
{

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    public int NumberOfPlayers { get => _numberOfPlayers; }
    private int _numberOfPlayers;

    /// <summary>
    /// We persist loaded models at the top level to ensure we only load them once #flyweight
    /// </summary>
    public Dictionary<string, ScaledModel> Models { get => _models; }
    private Dictionary<string, ScaledModel> _models;

    // map
    private Map _map;
    public Map Map { get => _map; }

    public int MapIndex { get => _mapIndex; }
    private int _mapIndex = 0;

    // scoring
    public ScoreState ScoreState { get => _scoreState; }
    private ScoreState _scoreState;

    public int[] Scores { get => _scores; }
    private int[] _scores;

    public int? RoundWinnerId { get => _roundWinnerId; }
    private int? _roundWinnerId = null;

    private float _roundFinishedAt = 0;

    public bool MatchFinished { get => _matchFinished; }
    private bool _matchFinished { get => _scores.Max() >= MaxPoints; }

    public const int MaxNumberOfPlayers = 4;
    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 4;

    public const int MaxPoints = 10;

    private const int roundTimeoutSec = 3;

    public Match(Game game, int NumberOfPlayers) : base(game)
    {
        _game = (GameMain)game;

        _numberOfPlayers = NumberOfPlayers;

        _models = new Dictionary<string, ScaledModel>();

        _scores = new int[_numberOfPlayers];

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.MATCH_UPDATE_ORDER;
        Visible = false;
    }

    protected override void LoadContent()
    {
        LoadMap();
    }

    private void LoadNextMap()
    {
        _mapIndex = (_mapIndex + 1) % numberOfMaps;
        LoadMap();
    }

    private void LoadMap()
    {
        // unloads the content for the current map before loading the next one
        if (Map != null)
        {
            // just reload all components and readd ourself
            GameMain.Components.Clear();
            GameMain.Components.Add(this);
            Models.Clear();
        }

        // initialize game overlay
        GameMain.Components.Add(new HudOverlay(GameMain));
        // GameMain.Components.Add(new ScoreboardOverlay(GameMain));

        _scoreState = ScoreState.None;

        string mapPath = string.Format("Content/Maps/{0}.txt", _mapIndex);
        _map = new Map(GameMain, GameMain.Services, mapPath);

        GameMain.Components.Add(_map);

        _roundFinishedAt = 0;
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        UpdateGameState(gameTime);
    }

    private void HandleInput()
    {
        if (Controls.ReloadMap.Pressed())
        {
            LoadMap();
        }

        if (Controls.NextMap.Pressed())
        {
            LoadNextMap();
        }
    }

    private void UpdateGameState(GameTime gameTime)
    {
        List<int> playersAlive = Map.PlayersAlive;
        if (_scoreState == ScoreState.None)
        {
            if (playersAlive.Count > 1)
            {
                return;
            }

            _roundFinishedAt = (float)gameTime.TotalGameTime.TotalSeconds;
            if (playersAlive.Count == 1)
            {
                _scoreState = ScoreState.Winner;
                _roundWinnerId = Map.PlayersAlive[0];
                _scores[(int)_roundWinnerId]++;
            }
            else if (playersAlive.Count == 0)
            {
                _scoreState = ScoreState.Draw;
            }
        }

        if (gameTime.TotalGameTime.TotalSeconds - _roundFinishedAt > roundTimeoutSec && !MatchFinished)
        {
            LoadNextMap();
        }
    }
}
