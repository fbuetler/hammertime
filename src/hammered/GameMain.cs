using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace hammered;

public class GameMain : Game
{
    private GraphicsDeviceManager _graphics;

    private BasicEffect _basicEffect;

    private Matrix _worldMatrix, _viewMatrix, _projectionMatrix;

    Model rubiksCubeModel;
    float blockSize = 10f;
    int xBlocks = 15;
    int zBlocks = 10;

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // setup our graphics scene matrices
        float xMapCenter = xBlocks / 2 * blockSize;
        float zMapCenter = zBlocks / 2 * blockSize;

        Vector3 cameraPosition = new Vector3(xMapCenter, 100f, zMapCenter + 75f);
        Vector3 cameraTarget = new Vector3(xMapCenter, 0f, zMapCenter); // look at middle of the tiles

        float fovAngle = MathHelper.ToRadians(45); // field of view
        float aspectRatio = (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight;
        float near = 0.01f;
        float far = 1000f;

        _worldMatrix = Matrix.Identity;
        _viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fovAngle, aspectRatio, near, far);

        // Setup our basic effect
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.World = _worldMatrix;
        _basicEffect.View = _viewMatrix;
        _basicEffect.Projection = _projectionMatrix;
        _basicEffect.VertexColorEnabled = true;

        rubiksCubeModel = Content.Load<Model>("RubiksCube");
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeys = Keyboard.GetState();

        //Press Esc To Exit
        if (currentKeys.IsKeyDown(Keys.Escape))
            this.Exit();


        //Press Directional Keys to rotate cube
        if (currentKeys.IsKeyDown(Keys.Up))
            _worldMatrix *= Matrix.CreateRotationX(-0.05f);
        if (currentKeys.IsKeyDown(Keys.Down))
            _worldMatrix *= Matrix.CreateRotationX(0.05f);
        if (currentKeys.IsKeyDown(Keys.Left))
            _worldMatrix *= Matrix.CreateRotationY(-0.05f);
        if (currentKeys.IsKeyDown(Keys.Right))
            _worldMatrix *= Matrix.CreateRotationY(0.05f);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        for (int i = 0; i < xBlocks; i++)
        {
            for (int j = 0; j < zBlocks; j++)
            {
                DrawModel(rubiksCubeModel, i, j);
            }
        }

        base.Draw(gameTime);
    }

    private void DrawModel(Model model, int i, int j)
    {
        Matrix translation = Matrix.CreateTranslation(i * blockSize, 0f, j * blockSize);

        foreach (ModelMesh mesh in rubiksCubeModel.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                //effect.EnableDefaultLighting();
                effect.AmbientLightColor = new Vector3(1f, 0, 0);
                effect.World = _worldMatrix;

                // translate tiles
                Matrix translatedView = new Matrix();
                Matrix.Multiply(ref translation, ref _viewMatrix, out translatedView);
                effect.View = translatedView;

                effect.Projection = _projectionMatrix;
            }
            mesh.Draw();
        }
    }
}
