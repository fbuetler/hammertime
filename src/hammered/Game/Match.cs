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

    /// <summary>
    /// We persist loaded models at the top level to ensure we only load them once #flyweight
    /// </summary>
    public Dictionary<string, ScaledModel> Models { get => _models; }
    private Dictionary<string, ScaledModel> _models;

    private Map _map;
    public Map Map { get => _map; }

    // game state
    public int MapIndex { get => _mapIndex; }
    private int _mapIndex = 0;

    public ScoreState ScoreState { get => _scoreState; }
    private ScoreState _scoreState;

    public int[] Scores { get => _scores; }
    private int[] _scores;

    public List<int> PlayersAlive { get => _playersAlive; }
    private List<int> _playersAlive;

    public int? WinnerId { get => _winnerId; }
    private int? _winnerId = null;

    private float _roundFinishedAt = 0;

    public int NumberOfPlayers { get => _numberOfPlayers; }
    private int _numberOfPlayers;

    private const int maxNumberOfPlayers = 4;
    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    private const int numberOfMaps = 4;

    private const int timeoutBetweenMaps = 3;

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

        if (Controls.Pause.Pressed())
        {
            foreach (Player p in Map.Players.Values)
            {
                p.Enabled = !p.Enabled;
            }
            foreach (Hammer h in Map.Hammers.Values)
            {
                h.Enabled = !h.Enabled;
            }
            foreach (Tile t in Map.Tiles)
            {
                if (t != null)
                    t.Enabled = !t.Enabled;
            }
        }
    }

    private void UpdateGameState(GameTime gameTime)
    {
        foreach (Player p in Map.Players.Values)
        {
            // TODO: (lmeinen) Wait with decreasing playsAlive until player hits ground below (could make for fun animation or items that allow one to come back from falling)
            if (p.State == PlayerState.DEAD || p.State == PlayerState.FALLING)
            {
                _playersAlive.Remove(p.PlayerId);
            }
        }

        if (_scoreState == ScoreState.None)
        {
            if (_playersAlive.Count > 1)
            {
                return;
            }

            _roundFinishedAt = (float)gameTime.TotalGameTime.TotalSeconds;
            if (_playersAlive.Count == 1)
            {
                _scoreState = ScoreState.Winner;
                _winnerId = _playersAlive[0];
                _scores[(int)_winnerId]++;
            }
            else if (_playersAlive.Count == 0)
            {
                _scoreState = ScoreState.Draw;
            }
        }


        if (gameTime.TotalGameTime.TotalSeconds - _roundFinishedAt > timeoutBetweenMaps)
        {
            LoadNextMap();
        }
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

        _scoreState = ScoreState.None;

        string mapPath = string.Format("Content/Maps/{0}.txt", _mapIndex);
        _map = new Map(GameMain, GameMain.Services, mapPath);

        GameMain.Components.Add(_map);

        _playersAlive = Map.Players.Keys.ToList();
        _roundFinishedAt = 0;
    }
}