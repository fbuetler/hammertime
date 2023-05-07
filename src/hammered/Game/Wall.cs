using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace hammered;

public class Wall : Tile
{

    public override TileState State => _state;
    private TileState _state;

    public override Dictionary<TileState, string> ObjectModelPaths { get => _objectModelPaths; }
    private Dictionary<TileState, string> _objectModelPaths;

    public override Vector3 MaxSize { get => _maxSize; }
    private static Vector3 _maxSize = new Vector3(1f, 2.79f, 1f);

    public Wall(Game game, Vector3 position) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.TILE_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.TILE_DRAW_ORDER;

        _state = TileState.HP100;

        _objectModelPaths = new Dictionary<TileState, string>();
        _objectModelPaths[TileState.HP100] = "Wall/iceWall4";
        _objectModelPaths[TileState.HP80] = "Wall/iceWall3";
        _objectModelPaths[TileState.HP60] = "Wall/iceWall2";
        _objectModelPaths[TileState.HP40] = "Wall/iceWall1";
        _objectModelPaths[TileState.HP20] = "Wall/iceWall0";
        _objectModelPaths[TileState.HP0] = "Wall/iceWall0";
    }
    public override void Update(GameTime gameTime)
    {
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

    private static TileState NextState(TileState tileState) => tileState switch
    {
        TileState.HP100 => TileState.HP60,
        TileState.HP60 => TileState.HP20,
        TileState.HP20 => TileState.HP0,
        TileState.HP0 => TileState.HP0,
        _ => throw new ArgumentOutOfRangeException(nameof(tileState), $"Unexpected tile state: {tileState}"),
    };
}