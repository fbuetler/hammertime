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

    public int OwnerID { get { return _owner.ID; } }
    private Player _owner;

    // hammer position
    private Vector3 _origin;
    public Vector2 Dir { get { return _dir; } }
    private Vector2 _dir;
    private Vector3 _pos;
    public float Speed { get { return _speed; } }
    private float _speed;

    // hammer state
    public bool IsFlying { get { return _isFlying; } }
    private bool _isFlying;
    public bool IsReturning { get { return _isReturning; } }
    private bool _isReturning;

    // hammer hit
    private bool[] _playerHit = new bool[] { false, false, false, false };

    public BoundingBox BoundingBox
    {
        get
        {
            return new BoundingBox(
                new Vector3(_pos.X, _pos.Y, _pos.Z),
                new Vector3(_pos.X + Hammer.Width, _pos.Y + Hammer.Height, _pos.Z + Hammer.Depth)
            );
        }
    }

    public Vector3 Center
    {
        get
        {
            return new Vector3(
                _pos.X + Width / 2,
                _pos.Y + Height / 2,
                _pos.Z + Depth / 2
            );
        }
    }

    public const float Width = 0.5f;
    public const float Height = 0.5f;
    public const float Depth = 0.5f;

    // constants for controlling throwing
    private const float ThrowSpeed = 20f;
    private const float MaxThrowDistance = 10f;
    private const float PickupDistance = 1f;

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
        _model = _map.Content.Load<Model>("Hammer/hammerCube");

        BoundingBox size = GetModelSize(_model);
        float xScale = Width / (size.Max.X - size.Min.X);
        float yScale = Height / (size.Max.Y - size.Min.Y);
        float zScale = Depth / (size.Max.Z - size.Min.Z);
        _modelScale = Matrix.CreateScale(xScale, yScale, zScale);
    }

    public void Reset(Player owner)
    {
        _owner = owner;
        _isFlying = false;
        _isReturning = false;
        _speed = ThrowSpeed;
        _playerHit = new bool[] { false, false, false, false };
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (!_isFlying)
        {
            return;
        }

        // if hammer is close to the player, it is picked up
        if (_isReturning && (Center - _owner.Center).LengthSquared() < PickupDistance)
        {
            PickUp();
        }

        // if max distance is reached, make it return
        if ((Center - _origin).Length() > MaxThrowDistance)
        {
            Return();
        }

        // if hammer is returning it should always follow its owner
        if (_isReturning)
        {
            FollowOwner();
        }

        // update position
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pos.X += _dir.X * ThrowSpeed * elapsed;
        _pos.Z += _dir.Y * ThrowSpeed * elapsed;
    }

    public void Throw(Vector2 direction)
    {
        if (!_isFlying && direction != Vector2.Zero)
        {
            _pos = new Vector3(
                _owner.Center.X - Width / 2,
                _owner.Center.Y - Height / 2,
                _owner.Center.Z - Depth / 2
            );
            _origin = _owner.Center;
            _dir = direction;

            _isFlying = true;
        }
    }

    private void Return()
    {
        // TODO (fbuetler) fix buggy return path (should follow player even if falling)
        _dir.X = _owner.Center.X - Center.X;
        _dir.Y = _owner.Center.Z - Center.Z;
        _dir.Normalize();
        _isReturning = true;
        _playerHit = new bool[] { false, false, false, false };
    }

    private void FollowOwner()
    {
        _dir.X = _owner.Center.X - Center.X;
        _dir.Y = _owner.Center.Z - Center.Z;
        _dir.Normalize();
    }

    private void PickUp()
    {
        _isFlying = false;
        _isReturning = false;
        _playerHit = new bool[] { false, false, false, false };
        _owner.OnHammerReturn();
    }

    public void OnHit(int id)
    {
        _playerHit[id] = true;
        Return();
    }

    public bool IsPlayerHit(int i)
    {
        return _playerHit[i];
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        if (!_isFlying || BoundingBox.Intersects(_owner.BoundingBox))
        {
            return;
        }

        // TODO (fbuetler) fix angle

        // as the model is rotate we have to
        // * move it into the origin
        // * rotate
        // * move it into it designated positions and also compensate for the move into the origin
        Matrix translateIntoOrigin = Matrix.CreateTranslation(
            -Width / 2,
            -Height / 2,
            -Depth / 2
        );

        float throwAngle = MathF.Atan2(_dir.Y, _dir.X);
        Matrix rotate = Matrix.CreateFromAxisAngle(Vector3.UnitY, throwAngle);

        Matrix translateIntoPosition = Matrix.CreateTranslation(
            _pos.X + Width / 2,
            _pos.Y + Height / 2,
            _pos.Z + Depth / 2
        );

        Matrix world = _modelScale * translateIntoOrigin * rotate * translateIntoPosition;
        DrawModel(_model, world, view, projection);

#if DEBUG
        // TODO (fbuetler) fix hitbox
        // world = rotate * Matrix.Identity;
        // _map.DebugDraw.Begin(world, view, projection);
        // _map.DebugDraw.DrawWireBox(BoundingBox, Color.Red);
        // _map.DebugDraw.End();
#endif
    }
}