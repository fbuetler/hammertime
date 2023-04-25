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

    public override Vector3 MaxSize { get => _maxSize; }
    private static Vector3 _maxSize = new Vector3(1f, 1f, 1f);

    public const float Width = 1f;
    public const float Height = 1f;
    public const float Depth = 1f;

    public Tile(Game game, Vector3 position) : base(game, position + _maxSize / 2)
    {
        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.TILE_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.TILE_DRAW_ORDER;

        _state = TileState.HP100;

        _objectModelPaths = new Dictionary<TileState, string>();
        _objectModelPaths[TileState.HP100] = "Tile/iceCube4";
        _objectModelPaths[TileState.HP80] = "Tile/iceCube3";
        _objectModelPaths[TileState.HP60] = "Tile/iceCube2";
        _objectModelPaths[TileState.HP40] = "Tile/iceCube1";
        _objectModelPaths[TileState.HP20] = "Tile/iceCube0";
        _objectModelPaths[TileState.HP0] = "Tile/iceCube0";

        _visitors = new HashSet<int>();
    }

    public override void Update(GameTime gameTime)
    {
        foreach (Player p in GameMain.Match.Map.Players.Values)
        {
            // is player standing on tile
            if (BoundingBox.Min.X <= p.Center.X && p.Center.X <= BoundingBox.Max.X &&
                BoundingBox.Min.Z <= p.Center.Z && p.Center.Z <= BoundingBox.Max.Z &&
                p.State != PlayerState.FALLING)
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

        foreach (Hammer h in GameMain.Match.Map.Hammers.Values)
        {
            // wall collisions
            if (h.BoundingBox.Intersects(BoundingBox) &&
                IntersectionDepth(h.BoundingBox, BoundingBox) != Vector3.Zero &&
                (h.State == HammerState.IS_FLYING || h.State == HammerState.IS_RETURNING))
            {
                _state = NextState(_state);
            }
        }

        if (_state == TileState.HP0)
        {
            // only called once
            OnBreak();
        }
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
        this.Visible = false;
        this.Enabled = false;
    }
}