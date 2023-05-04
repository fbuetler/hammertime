using System;
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

    public int NumberOfRounds { get => _numberOfRounds; }
    private int _numberOfRounds;

    /// <summary>
    /// We persist loaded models at the top level to ensure we only load them once #flyweight
    /// </summary>
    public Dictionary<string, ScaledModel> Models { get => _models; }
    private Dictionary<string, ScaledModel> _models;

    private HudOverlay _hud;
    private StartOverlay _startOverlay;
    private WinnerOverlay _roundDrawOverlay;
    private WinnerOverlay[] _roundWinnerOverlays;
    private WinnerOverlay[] _matchWinnerOverlays;
    private ScoreboardOverlay _scoreboardOverlay;

    public PauseOverlay PauseOverlay { get => _pauseOverlay; }
    private PauseOverlay _pauseOverlay;

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

    private float _roundStartedAt = 0;
    private float _roundFinishedAt = 0;

    private bool _roundStarted;
    public bool MatchFinished { get => _scores.Max() >= _numberOfRounds; }

    public const int MaxNumberOfPlayers = 4;
    public const int MaxNumberOfRounds = 10;
    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 11;

    private const int startDelayMs = 1000;
    private const int finishedDelayMs = 2000;
    private const int nextRoundTimeoutMs = 4000;

    public Match(Game game, int numberOfPlayers, int numberOfRounds) : base(game)
    {
        _game = (GameMain)game;

        _numberOfPlayers = numberOfPlayers;
        _numberOfRounds = numberOfRounds;

        _models = new Dictionary<string, ScaledModel>();

        _scores = new int[_numberOfPlayers];

        _roundWinnerOverlays = new WinnerOverlay[_numberOfPlayers];
        _matchWinnerOverlays = new WinnerOverlay[_numberOfPlayers];

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.MATCH_UPDATE_ORDER;
        Visible = false;
    }

    protected override void LoadContent()
    {
        _hud = new HudOverlay(GameMain);
        _startOverlay = new StartOverlay(GameMain);
        _pauseOverlay = new PauseOverlay(GameMain);
        _roundDrawOverlay = new WinnerOverlay(GameMain, "Round/draw");
        for (int i = 0; i < _roundWinnerOverlays.Count(); i++)
        {
            _roundWinnerOverlays[i] = new WinnerOverlay(GameMain, $"Round/{i}");
        }
        for (int i = 0; i < _matchWinnerOverlays.Count(); i++)
        {
            _matchWinnerOverlays[i] = new WinnerOverlay(GameMain, $"Match/{i}");
        }
        _scoreboardOverlay = new ScoreboardOverlay(GameMain);

        _mapIndex = GameMain.Random.Next(numberOfMaps);
        LoadMap();
    }

    private void LoadNextMap()
    {
        _mapIndex = GameMain.Random.Next(numberOfMaps);
        LoadMap();
    }

    public void LoadMap()
    {
        // unloads the content for the current map before loading the next one
        if (Map != null)
        {
            // just reload all components and readd ourself
            GameMain.Components.Clear();
            GameMain.Components.Add(this);
            Models.Clear();
        }

        // initialize overlays
        _startOverlay.Visible = true;
        GameMain.Components.Add(_startOverlay);

        _pauseOverlay.Visible = false;
        GameMain.Components.Add(_pauseOverlay);

        GameMain.Components.Add(_scoreboardOverlay);

        _roundDrawOverlay.Visible = false;
        GameMain.Components.Add(_roundDrawOverlay);

        foreach (var roundWinnerOverlay in _roundWinnerOverlays)
        {
            roundWinnerOverlay.Visible = false;
            GameMain.Components.Add(roundWinnerOverlay);
        }
        foreach (var gameWinnerOverlay in _matchWinnerOverlays)
        {
            gameWinnerOverlay.Visible = false;
            GameMain.Components.Add(gameWinnerOverlay);
        }

        // init map
        string mapPath = string.Format("Content/Maps/{0}.txt", _mapIndex);
        _map = new Map(GameMain, GameMain.Services, mapPath);
        GameMain.Components.Add(_map);

        _scoreState = ScoreState.None;
        _roundStarted = false;
        _roundStartedAt = 0;
        _roundFinishedAt = 0;
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        if (!_roundStarted)
            StartRound(gameTime);
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

        if (MatchFinished)
        {
            if (Controls.Interact.Pressed())
            {
                GameMain.EndMatch();
            }
        }
    }

    private void StartRound(GameTime gameTime)
    {
        float elapsedSinceStart = (float)gameTime.TotalGameTime.TotalMilliseconds;

        if (_roundStartedAt == 0)
        {
            _roundStartedAt = elapsedSinceStart;
        }

        if (_roundStartedAt > 0 && elapsedSinceStart - _roundStartedAt > startDelayMs)
        {
            _startOverlay.Visible = false;
            _roundStarted = true;

            foreach (Player p in Map.Players.Values)
            {
                p.Enabled = true;
            }
            foreach (Hammer h in Map.Hammers.Values)
            {
                h.Enabled = true;
            }
            foreach (Arrow a in Map.Arrows.Values)
            {
                a.Enabled = true;
            }
            foreach (Tile t in Map.Tiles)
            {
                if (t != null)
                    t.Enabled = true;
            }
        }
    }

    private void UpdateGameState(GameTime gameTime)
    {
        float totalElapsed = (float)gameTime.TotalGameTime.TotalMilliseconds;

        List<int> playersAlive = Map.PlayersAlive;
        switch (GameMain.Match.ScoreState)
        {
            case ScoreState.None:
                if (playersAlive.Count > 1)
                {
                    return;
                }

                _roundFinishedAt = totalElapsed;
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
                break;
            case ScoreState.Winner:
                if (totalElapsed - _roundFinishedAt > finishedDelayMs)
                    _roundWinnerOverlays[(int)_roundWinnerId].Visible = true;
                break;
            case ScoreState.Draw:
                if (totalElapsed - _roundFinishedAt > finishedDelayMs)
                    _roundDrawOverlay.Visible = true;
                break;
            default:
                throw new NotSupportedException(String.Format("Scorestate type '{0}' is not supported", ScoreState));
        }

        if (totalElapsed - _roundFinishedAt > nextRoundTimeoutMs)
        {
            _roundDrawOverlay.Visible = false;
            _roundWinnerOverlays.ToList().ForEach(o => o.Visible = false);

            if (MatchFinished)
            {
                int winnerId = _scores.ToList().IndexOf(_scores.Max());
                _matchWinnerOverlays[winnerId].Visible = true;
            }
            else
            {
                LoadNextMap();
                _roundWinnerOverlays.ToList().ForEach(o => o.Visible = false);
            }
        }
    }
}
