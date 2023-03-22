using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public class Hammer : GameObject
{

    private Map _map;

    private Model _model;
    private Matrix _modelScale;

    // hammer state
    private Player _owner;
    private Vector2 _dir;
    private Vector3 _pos;
    private Vector3 _origin;
    private bool _isThrown;
    private bool _isReturning;

    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(
                new Vector3(_pos.X, _pos.Y, _pos.Z),
                new Vector3(_pos.X + Player.Width, _pos.Y + Player.Height, _pos.Z + Player.Depth)
            );
        }
    }

    public const float Width = 0.5f;
    public const float Height = 0.5f;
    public const float Depth = 0.5f;

    // constants for controlling throwing
    private const float ThrowSpeed = 10f;
    private const float MaxThrowDistance = 5f;

    // TODO (fbuetler) deacclerate when close to player on return/before hit

    public Hammer(Map map, Player owner)
    {
        if (map == null)
            throw new ArgumentNullException("map");
        if (owner == null)
            throw new ArgumentNullException("owner");

        _map = map;

        LoadContent();

        Reset(owner);
    }

    public void LoadContent()
    {
        _model = _map.Content.Load<Model>("cube");

        BoundingBox size = GetModelSize(_model);
        float xScale = Width / (size.Max.X - size.Min.X);
        float yScale = Height / (size.Max.Y - size.Min.Y);
        float zScale = Depth / (size.Max.Z - size.Min.Z);
        _modelScale = Matrix.CreateScale(xScale, yScale, zScale);
    }

    public void Reset(Player owner)
    {
        _owner = owner;
        _isThrown = false;
        _isReturning = false;
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (!_isThrown)
        {
            return;
        }

        if (_isReturning && (_pos - _owner.Position).LengthSquared() < 1)
        {
            _isThrown = false;
            _isReturning = false;
            _owner.OnHammerReturn();
        }

        if ((_pos - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
        {
            // TODO (fbuetler) fix buggy return path (should follow player even if falling)
            _dir.X = _owner.Position.X - _pos.X;
            _dir.Y = _owner.Position.Z - _pos.Z;
            _dir.Normalize();
            _isReturning = true;
        }

        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pos.X += _dir.X * elapsed * ThrowSpeed;
        _pos.Z += _dir.Y * elapsed * ThrowSpeed;
    }

    public void Throw(Vector2 direction)
    {
        if (!_isThrown && direction != Vector2.Zero)
        {
            _pos = _owner.Position;
            _origin = _pos;
            _dir = direction;

            _isThrown = true;
        }
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        if (!_isThrown)
        {
            return;
        }

        // TODO (fbuetler) fix angle
        float throwAngle = (float)Math.Atan(_dir.Y / _dir.X);
        Quaternion rotationQuaterion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)throwAngle);

        Matrix rotation = Matrix.CreateFromQuaternion(rotationQuaterion);
        Matrix translation = Matrix.CreateTranslation(_pos);

        Matrix world = _modelScale * rotation * translation;
        DrawModel(_model, world, view, projection);

        // TODO (fbuetler) fix hitbox
        world = rotation * Matrix.Identity;
        // _map.DebugDraw.Begin(world, view, projection);
        // _map.DebugDraw.DrawWireBox(BoundingBox, Color.Red);
        // _map.DebugDraw.End();
    }
}