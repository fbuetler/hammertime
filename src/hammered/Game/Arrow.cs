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

    //private int _playerId;
    //public int PlayerId { get => _playerId; }
    
    public Vector3 _chargeArrow;
    public override Vector3 Size { get => _chargeArrow; set => _chargeArrow = value;}

    private ArrowState _state;
    public override ArrowState State { get => _state; }

    // TODO: (lmeinen) why isn't this working?
    public override Vector3 RotCenter { get => Position; }

    private Dictionary<ArrowState, string> _objectModelPaths;
    public override Dictionary<ArrowState, string> ObjectModelPaths { get => _objectModelPaths; }

    public Arrow(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        Visible = false;

        _ownerId = ownerId;

        _state = ArrowState.IS_NOT_CHARGING;
        _chargeArrow = new Vector3(5f, 0.1f, 0.5f);

        _objectModelPaths = new Dictionary<ArrowState, string>();
        _objectModelPaths[ArrowState.IS_CHARGING] = "Hammer/hammerCube";
        _objectModelPaths[ArrowState.IS_NOT_CHARGING] = "Hammer/hammerCube";
    }

    public override void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float throwDistance;
        Vector3 pos;
        switch (_state)
        {
            case ArrowState.IS_NOT_CHARGING when GameMain.Map.Players[OwnerId].State == PlayerState.CHARGING:
                _state = ArrowState.IS_CHARGING;
                Direction = GameMain.Map.Hammers[OwnerId].Direction;
                pos = GameMain.Map.Players[OwnerId].Center;
                throwDistance = GameMain.Map.Players[OwnerId].Charge();
                _chargeArrow = new Vector3(throwDistance/(float) 2, 0.1f, 0.5f);
                pos.Y = 1f; // arrow should be on the floor
                Position = pos;
                Visible = true;
                break;
            case ArrowState.IS_CHARGING when GameMain.Map.Players[OwnerId].State != PlayerState.CHARGING:
                _state = ArrowState.IS_NOT_CHARGING;
                Visible = false;
                break;
            case ArrowState.IS_CHARGING:
                Direction = GameMain.Map.Hammers[OwnerId].Direction;
                throwDistance = GameMain.Map.Players[OwnerId].Charge();
                _chargeArrow = new Vector3(throwDistance/(float) 2, 0.1f, 0.5f);
                pos = GameMain.Map.Players[OwnerId].Center;
                pos.Y = 1f; // arrow should be on the floor
                Position = pos;
                break;
            default:
                // do nothing
                break;
        }
    }

}