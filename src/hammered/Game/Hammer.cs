using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

namespace hammered;

public struct HammerThrow
{
    public Player Owner;
    public BoundingBox BoundingBox;
    public Vector2 Direction;
    public bool IsFlying;

    public HammerThrow(Player o, BoundingBox b, Vector2 d, bool f)
    {
        Owner = o;
        BoundingBox = b;
        Direction = d;
        IsFlying = f;
    }
}

public class Hammer : GameObject
{

    private Map _map;

    private Model _model;
    private Matrix _modelScale;

    // hammer state
    private Player _owner;
    private Vector3 _origin;
    private Vector2 _dir;
    public Vector2 Dir { get { return _dir; } }
    private Vector3 _pos;
    private bool _isFlying;
    public bool IsReturning { get { return _isReturning; } }
    private bool _isReturning;
    private bool _isDeleted;
    private bool[] _playerHit = new bool[] {false,false,false,false};
    private float[] _hitTimes = new float[] { 0f, 0f, 0f, 0f };
    private float[] _hitX = new float[] { 0f, 0f, 0f, 0f };
    private float[] _hitZ = new float[] { 0f, 0f, 0f, 0f };
    public void HitPlayer(int id, float time, float x, float z) { _playerHit[id] = true; _hitTimes[id] = time; _hitX[id] = x; _hitZ[id] = z; }
    public bool CheckHit(int i) { return _playerHit[i];  }
    public bool CheckDist(int id, float x, float z, float maxdist) { return (float)Math.Sqrt(((_hitX[id] - x) * (_hitX[id] - x)) + ((_hitZ[id] - z) * (_hitZ[id] - z))) > maxdist; }
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

    public const float Width = 0.5f;
    public const float Height = 0.5f;
    public const float Depth = 0.5f;

    // constants for controlling throwing
    private const float _throwSpeed = 20f;
    public float ThrowSpeed { get { return _throwSpeed; } }
    private const float MaxThrowDistance = 20f;
    private float _speed = 0f;
    public float Speed { get { return _speed; } }
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
        //TODO(fred) replace the speed with some actual speed. not just a fixed number
        _speed = _throwSpeed;
        _playerHit = new bool[] { false, false, false, false };
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (!_isFlying)
        {
            return;
        }

        // hammer is close to the player, it is returned
        if (_isReturning && (_pos - _owner.Position).LengthSquared() < 1f)
        {
            _isFlying = false;
            _isReturning = false;
            _playerHit = new bool[] { false, false, false, false };
            _owner.OnHammerReturn();
        }

        // if max distance is reached, make it return
        if ((_pos - _origin).LengthSquared() > MaxThrowDistance * MaxThrowDistance)
        {
            // TODO (fbuetler) fix buggy return path (should follow player even if falling)
            _dir.X = _owner.Position.X - _pos.X;
            _dir.Y = _owner.Position.Z - _pos.Z;
            _dir.Normalize();
            _isReturning = true;
            _playerHit = new bool[] { false, false, false, false };
        }

        // if hammer is returning it should always go to player
        if (_isReturning)
        {
            _dir.X = _owner.Position.X - _pos.X;
            _dir.Y = _owner.Position.Z - _pos.Z;
            _dir.Normalize();
        }

        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pos.X += _dir.X * elapsed * _throwSpeed;
        _pos.Z += _dir.Y * elapsed * _throwSpeed;
    }

    public void Throw(Vector2 direction)
    {
        if (!_isFlying && direction != Vector2.Zero)
        {
            _pos = _owner.Position;
            _origin = _pos;
            _dir = direction;

            _isFlying = true;
        }
    }

    public HammerThrow GetHammerThrow()
    {
        return new HammerThrow(_owner, BoundingBox, _dir, _isFlying);
    }

    public override void Draw(Matrix view, Matrix projection)
    {
        if (!_isFlying)
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