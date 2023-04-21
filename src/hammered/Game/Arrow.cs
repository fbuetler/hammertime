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
    public Vector3 _maxSize = new Vector3(5f, 0.1f, 0.5f);

    public override ArrowState State { get => _state; }
    private ArrowState _state;

    public override Dictionary<ArrowState, string> ObjectModelPaths { get => _objectModelPaths; }
    private Dictionary<ArrowState, string> _objectModelPaths;

    private float _throwDistance;

    private const float MaxArrowLength = 15f;

    public Arrow(Game game, Vector3 position, int ownerId) : base(game, position)
    {
        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.ARROW_UPDATE_ORDER;
        Visible = false;
        DrawOrder = GameMain.ARROW_DRAW_ORDER;

        _ownerId = ownerId;

        _state = ArrowState.IS_NOT_CHARGING;

        _objectModelPaths = new Dictionary<ArrowState, string>();
        _objectModelPaths[ArrowState.IS_CHARGING] = "Hammer/hammerCube";
        _objectModelPaths[ArrowState.IS_NOT_CHARGING] = "Hammer/hammerCube";
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
                pos.Y = 1f; // arrow should be on the floor
                Center = pos;

                // arrow direction
                Direction = GameMain.Match.Map.Hammers[OwnerId].Direction;

                // arrow length
                _throwDistance = GameMain.Match.Map.Players[OwnerId].ThrowDistance;
                break;
            default:
                // do nothing
                break;
        }
    }

    // TODO (fbuetler) fix rotation center once we have the arrow model and dont override
    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Match.Map.Camera.View;
        Matrix projection = GameMain.Match.Map.Camera.Projection;

        ScaledModel scaledModel = GameMain.Match.Models[State.ToString()];

        Vector3 rotationCenter = new Vector3(0, 0, Size.Z / 2);
        Vector3 position = Center + rotationCenter;

        Matrix translateIntoOrigin = Matrix.CreateTranslation(-rotationCenter);
        Matrix scale = Matrix.CreateScale(MathF.Min(_throwDistance * MaxArrowLength / Hammer.MaxThrowDistance, MaxArrowLength), 1, 3);
        Matrix rotate = ComputeRotation();
        Matrix translateIntoPosition = Matrix.CreateTranslation(position);
        Matrix world = scaledModel.modelScale * translateIntoOrigin * scale * rotate * translateIntoPosition;

        DrawModel(scaledModel.model, world, view, projection);

#if DEBUG
        GameMain.Match.Map.DebugDraw.Begin(Matrix.Identity, view, projection);
        GameMain.Match.Map.DebugDraw.DrawWireBox(BoundingBox, GetDebugColor());
        GameMain.Match.Map.DebugDraw.End();
#endif
    }
}