using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

    public override TileState State => _state;
    private TileState _state;

    public override Dictionary<TileState, string> ObjectModelPaths { get => _objectModelPaths; }
    private Dictionary<TileState, string> _objectModelPaths;

    public override Vector3 Size { get => _sizeVec; set => _sizeVec = value;}
    private Vector3 _sizeVec = new Vector3(Width, Height, Depth);

    public const float Width = 1f;
    public const float Height = 1f;
    public const float Depth = 1f;

    public Tile(Game game, Vector3 position, Boolean isBroken) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = !isBroken;

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
        foreach (Player p in GameMain.Map.Players.Values)
        {
            // is player standing on tile
            if (BoundingBox.Contains(p.Center - Vector3.UnitY).Equals(ContainmentType.Contains) && p.State != PlayerState.FALLING)
            {
                if (!_visitors.Contains(p.PlayerId))
                {
                    OnEnter(p);
                }
            }
            else
            {
                if (_visitors.Contains(p.PlayerId))
                {
                    OnExit(p);
                }
            }
        }

        foreach (Hammer h in GameMain.Map.Hammers.Values)
        {
            // wall collisions
            if (h.BoundingBox.Intersects(BoundingBox) && h.State != HammerState.IS_HELD)
            {
                // TODO (fbuetler) maybe decrease health instead of directly destroying it
                _state = TileState.HP0;
            }
        }

        if (_state == TileState.HP0)
        {
            // only called once
            OnBreak();
        }

        // TODO (fbuetler) update breaking animation based on health points
    }

    public void OnEnter(Player player)
    {
        _visitors.Add(player.PlayerId);
    }

    public void OnExit(Player player)
    {
        _visitors.Remove(player.PlayerId);
        _state = NextState(_state);
    }

    private static TileState NextState(TileState tileState) => tileState switch
    {
        // TODO: (lmeinen) Wouldn't it be cooler if we used this everywhere, using case guards and callable actions?
        TileState.HP100 => TileState.HP80,
        TileState.HP80 => TileState.HP60,
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
        this.Enabled = false;
    }
}