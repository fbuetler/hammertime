using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum TileState
{
    HP100,
    HP80,
    HP60,
    HP40,
    HP20,
    HP0
}

public class Tile : GameObject<TileState>
{

    private HashSet<int> _visitors;

    private TileState _state;
    public override TileState State => _state;

    private Dictionary<TileState, string> _objectModelPaths;
    public override Dictionary<TileState, string> ObjectModelPaths => _objectModelPaths;

    public override Vector3 Size => new Vector3(1f, 1f, 1f);

    public const float Width = 1f;
    public const float Height = 1f;
    public const float Depth = 1f;

    private const float maxHealthPoints = 90f;
    private const float damage = 30f;
    private const float healthLevel = 20f;

    public Tile(Game game, Vector3 position, Boolean isBroken) : base(game, position)
    {
        this.Enabled = true;
        this.Visible = !isBroken;

        _state = isBroken ? TileState.HP0 : TileState.HP100;
        _objectModelPaths = new Dictionary<TileState, string>();
        _objectModelPaths[TileState.HP100] = "Tile/tileCube4";
        _objectModelPaths[TileState.HP80] = "Tile/tileCube3";
        _objectModelPaths[TileState.HP60] = "Tile/tileCube2";
        _objectModelPaths[TileState.HP40] = "Tile/tileCube1";
        _objectModelPaths[TileState.HP20] = "Tile/tileCube0";
        _objectModelPaths[TileState.HP0] = "Tile/tileCube0";

        _visitors = new HashSet<int>();
    }

    public override void Update(GameTime gameTime)
    {
        // TODO (fbuetler) update breaking animation based on health points
    }

    public void OnEnter(Player player)
    {
        if (_visitors.Contains(player.PlayerId))
        {
            return;
        }

        _visitors.Add(player.PlayerId);
    }

    public void OnExit(Player player)
    {
        if (!_visitors.Contains(player.PlayerId))
        {
            return;
        }

        _visitors.Remove(player.PlayerId);

        _state = NextState(_state);
    }

    private static TileState NextState(TileState tileState) => tileState switch
    {
        // TODO: (lmeinen) Wouldn't it be cooler if we used this everywhere, using case guards and callable actions?
        TileState.HP100 => TileState.HP80,
        TileState.HP80 => TileState.HP40,
        TileState.HP60 => TileState.HP40,
        TileState.HP40 => TileState.HP20,
        TileState.HP20 => TileState.HP0,
        TileState.HP0 => TileState.HP0,
        _ => throw new ArgumentOutOfRangeException(nameof(tileState), $"Unexpected tile state: {tileState}"),
    };

    public void OnBreak()
    {
        // TODO (fbuetler) make invisible i.e. change/remove texture
        this.Visible = false;
    }
}