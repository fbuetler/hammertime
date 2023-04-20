using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

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

    public override Vector3 MaxSize { get => _maxSize; set => _maxSize = value; }
    public Vector3 _maxSize = new Vector3(5f, 0.1f, 0.5f);

    private ArrowState _state;
    public override ArrowState State { get => _state; }

    // TODO: (lmeinen) why isn't this working?
    public override Vector3 RotCenter { get => Center; }

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
        float throwDistance;
        Vector3 pos;
        switch (_state)
        {
            case ArrowState.IS_NOT_CHARGING when GameMain.Match.Map.Players[OwnerId].State == PlayerState.CHARGING:
                _state = ArrowState.IS_CHARGING;
                Direction = GameMain.Match.Map.Hammers[OwnerId].Direction;
                pos = GameMain.Match.Map.Players[OwnerId].Center;
                throwDistance = GameMain.Match.Map.Players[OwnerId].Charge;
                _maxSize = new Vector3(throwDistance / (float)2, 0.1f, 0.5f);
                pos.Y = 1f; // arrow should be on the floor
                Center = pos;
                Visible = true;
                break;
            case ArrowState.IS_CHARGING when GameMain.Match.Map.Players[OwnerId].State != PlayerState.CHARGING:
                _state = ArrowState.IS_NOT_CHARGING;
                Visible = false;
                break;
            case ArrowState.IS_CHARGING:
                Direction = GameMain.Match.Map.Hammers[OwnerId].Direction;
                throwDistance = GameMain.Match.Map.Players[OwnerId].Charge;
                _maxSize = new Vector3(throwDistance / (float)2, 0.1f, 0.5f);
                pos = GameMain.Match.Map.Players[OwnerId].Center;
                pos.Y = 1f; // arrow should be on the floor
                Center = pos;
                break;
            default:
                // do nothing
                break;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Match.Map.Camera.View;
        Matrix projection = GameMain.Match.Map.Camera.Projection;

        Matrix rotate = ComputeRotation();

        Matrix translateIntoPosition = Matrix.CreateTranslation(RotCenter);

        Matrix world = Matrix.CreateScale(5f) * GameMain.Match.Models[State.ToString()].modelScale * rotate * translateIntoPosition;

        DrawModel(GameMain.Match.Models[State.ToString()].model, world, view, projection);

#if DEBUG
        GameMain.Match.Map.DebugDraw.Begin(Matrix.Identity, view, projection);
        GameMain.Match.Map.DebugDraw.DrawWireBox(BoundingBox, GetDebugColor());
        GameMain.Match.Map.DebugDraw.End();
#endif
    }

    private Matrix ComputeRotation()
    {
        float angle = MathF.Atan2(-Direction.Z, Direction.X);
        Matrix rotate = Matrix.CreateFromAxisAngle(Vector3.UnitY, angle);
        return rotate;
    }

    private void DrawModel(Model model, Matrix world, Matrix view, Matrix projection)
    {
        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.EnableDefaultLighting();
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;
            }

            mesh.Draw();
        }
    }

}