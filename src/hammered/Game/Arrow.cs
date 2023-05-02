using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;

public enum ArrowState
{
    IS_CHARGING,
    IS_NOT_CHARGING
}

public class Arrow : GameObject<ArrowState>
{

    public int OwnerId { get => _ownerId; }
    private int _ownerId;

    public override Vector3 MaxSize { get => _maxSize; }
    public Vector3 _maxSize = new Vector3(1f, 1f, 1f);

    public override ArrowState State { get => _state; }
    private ArrowState _state;

    public override Dictionary<ArrowState, string> ObjectModelPaths { get => _objectModelPaths; }
    private Dictionary<ArrowState, string> _objectModelPaths;

    private float _throwDistance;

    private const float MaxArrowLength = 5f;

    public Arrow(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = false; // enabled by match
        UpdateOrder = GameMain.ARROW_UPDATE_ORDER;
        Visible = false;
        DrawOrder = GameMain.ARROW_DRAW_ORDER;

        _ownerId = ownerId;

        _state = ArrowState.IS_NOT_CHARGING;

        _objectModelPaths = new Dictionary<ArrowState, string>();
        _objectModelPaths[ArrowState.IS_CHARGING] = "Player/arrow";
        _objectModelPaths[ArrowState.IS_NOT_CHARGING] = "Player/arrow";
    }

    public override void Update(GameTime gameTime)
    {
        switch (_state)
        {
            case ArrowState.IS_NOT_CHARGING when GameMain.Match.Map.Players[OwnerId].State == PlayerState.CHARGING:
                _state = ArrowState.IS_CHARGING;
                Visible = true;
                break;
            case ArrowState.IS_CHARGING when GameMain.Match.Map.Players[OwnerId].State != PlayerState.CHARGING:
                _state = ArrowState.IS_NOT_CHARGING;
                Visible = false;
                break;
            case ArrowState.IS_CHARGING:
                // arrow origin
                Vector3 pos = GameMain.Match.Map.Players[OwnerId].Center;
                pos.Y = 1.1f; // arrow should be on the floor
                Center = pos;

                // arrow direction
                Direction = GameMain.Match.Map.Hammers[OwnerId].AimingDirection();

                // arrow length
                _throwDistance = GameMain.Match.Map.Players[OwnerId].ThrowDistance;
                break;
            default:
                // do nothing
                break;
        }
    }

    protected override Matrix ComputeScale()
    {
        // scale along x-axis based on charged amount
        return Matrix.CreateScale(_throwDistance * MaxArrowLength / Hammer.MaxThrowDistance, 1, 1);
    }

    protected override void SetCustomLightingProperties(BasicEffect effect)
    {
        base.SetCustomLightingProperties(effect);

        // change colour based on charged amount
        effect.AmbientLightColor = Vector3.One * _throwDistance;
    }
}