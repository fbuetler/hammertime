using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public enum ArrowState
{
    IS_CHARGING,
    IS_NOT_CHARGING
}

public class Arrow : GameObject<ArrowState>
{

    private int _ownerId;
    public int OwnerId { get => _ownerId; }

    public override Vector3 Size { get => new Vector3(3f, 0.1f, 0.5f); }

    private ArrowState _state;
    public override ArrowState State { get => _state; }

    private Dictionary<ArrowState, string> _objectModelPaths;
    public override Dictionary<ArrowState, string> ObjectModelPaths { get => _objectModelPaths; }

    public Arrow(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = false;

        _ownerId = ownerId;

        _state = ArrowState.IS_NOT_CHARGING;

        _objectModelPaths = new Dictionary<ArrowState, string>();
        _objectModelPaths[ArrowState.IS_CHARGING] = "Hammer/hammerCube";
        _objectModelPaths[ArrowState.IS_NOT_CHARGING] = "Hammer/hammerCube";
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case ArrowState.IS_NOT_CHARGING when GameMain.Map.Players[OwnerId].State == PlayerState.CHARGING:
                _state = ArrowState.IS_CHARGING;
                Direction = GameMain.Map.Hammers[OwnerId].Direction;
                Position = GameMain.Map.Players[OwnerId].Position;
                Visible = true;
                break;
            case ArrowState.IS_CHARGING when GameMain.Map.Players[OwnerId].State != PlayerState.CHARGING:
                _state = ArrowState.IS_NOT_CHARGING;
                Visible = false;
                break;
            case ArrowState.IS_CHARGING:
                Direction = GameMain.Map.Hammers[OwnerId].Direction;
                Position = GameMain.Map.Players[OwnerId].Position;
                break;
            default:
                // do nothing
                break;
        }
    }
}