using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public class Player : DrawableGameComponent
{
    public int ID
    {
        get { return _id; }
    }
    int _id;

    public Circle BoundingTopDownCircle
    {
        get
        {
            return new Circle(new Vector2(_pos.X + 0.5f, _pos.Z + 0.5f), 0.5f);
        }
    }

    private Map _map;

    private Model _model;

    private Vector3 _pos;

    private Vector2 _movement;

    private Vector3 _velocity;

    public Boolean IsStandingOnTile
    {
        set { _isFalling = _isFalling || !value; }
    }
    private Boolean _isFalling;

    // Constants for controlling horizontal movement
    private const float MoveAcceleration = 1300f;
    private const float MaxMoveSpeed = 175f;
    private const float GroundDragFactor = 0.48f;
    private const float AirDragFactor = 0.58f;

    // Constants for controlling vertical movement
    private const float GravityAcceleration = 960f;
    private const float MaxFallSpeed = 340f;

    public Player(Game game, Map map, int id, Vector3 position) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");

        if (map == null)
            throw new ArgumentNullException("map");

        _map = map;
        _pos = new Vector3(position.X, position.Y, position.Z);
        _model = _map.Content.Load<Model>("RubiksCube");
        _id = id;
        _isFalling = false;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, GamePadState gamePadState)
    {
        GetInput(keyboardState, gamePadState);

        ApplyPhysics(gameTime);

        // clear input
        _movement = new Vector2(0, 0);
    }

    private void GetInput(KeyboardState keyboardState, GamePadState gamePadState)
    {
        if (keyboardState.IsKeyDown(Keys.Up))
        {
            _movement.Y -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.Down))
        {
            _movement.Y += 1.0f;
        }

        if (keyboardState.IsKeyDown(Keys.Left))
        {
            _movement.X -= 1.0f;
        }
        else if (keyboardState.IsKeyDown(Keys.Right))
        {
            _movement.X += 1.0f;
        }
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector3 previousPos = _pos;

        // base velocity is a combination of horizontal movement control and
        // acceleration downward due to gravity
        _velocity.X += _movement.X * MoveAcceleration * elapsed;
        _velocity.Z += _movement.Y * MoveAcceleration * elapsed;
        _velocity.Y = MathHelper.Clamp(_velocity.Y - GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

        _velocity.Y = WalkOffMap(gameTime, _velocity.Y);

        if (!_isFalling)
        {
            _velocity *= GroundDragFactor;
        }
        else
        {
            _velocity *= AirDragFactor;
        }

        // prevent the player from running faster than his top speed
        if (Math.Abs(_movement.X) > MaxMoveSpeed / 2 && Math.Abs(_movement.Y) > MaxMoveSpeed / 2)
        {
            _movement.Normalize();
        }

        // apply velocity
        _pos += _velocity * elapsed;

        // if the player is now colliding with the map, separate them.
        HandleCollisions();

        // if the collision stopped us from moving, reset the velocity to zero
        if (_pos.X == previousPos.X)
            _velocity.X = 0;

        if (_pos.Y == previousPos.Y)
            _velocity.Y = 0;

        if (_pos.Z == previousPos.Z)
            _velocity.Z = 0;

    }

    private float WalkOffMap(GameTime gameTime, float velocityY)
    {
        // TODO (fbuetler) check if off map
        if (_isFalling)
        {
            return velocityY;
        }

        return 0;
    }

    private void HandleCollisions()
    {
        // TODO (fbuetler) seperate player from map elements
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix translation = Matrix.CreateTranslation(_pos);

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.AmbientLightColor = new Vector3(1f, 0, 0);
                effect.World = _map.Camera.WorldMatrix;

                // translate tiles
                Matrix translatedView = new Matrix();
                Matrix viewMatrix = _map.Camera.ViewMatrix;
                Matrix.Multiply(ref translation, ref viewMatrix, out translatedView);
                effect.View = translatedView;

                effect.Projection = _map.Camera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }
}