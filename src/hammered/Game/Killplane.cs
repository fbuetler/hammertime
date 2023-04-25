using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammered;


public class Clouds : DrawableGameComponent
{
    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private Texture2D[] _textures = new Texture2D[NumLayers];

    private Vector3[] _plane;

    private const int NumLayers = 3;

    public Clouds(Game game, float height) : base(game)
    {
        _game = (GameMain)game;

        // set plane positions
        _plane = new Vector3[] {
            new Vector3(-20f, height, -20f),
            new Vector3(-20f, height, +50f),
            new Vector3(+50f, height, -20f),
            new Vector3(+50f, height, +50f),
        };

        // make update and draw called by monogame
        Enabled = true;
        UpdateOrder = GameMain.KILLPLANE_UPDATE_ORDER;
        Visible = true;
        DrawOrder = GameMain.KILLPLANE_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        // Load textures 
        for (int l = 0; l < NumLayers; l++)
        {
            _textures[l] = GameMain.Content.Load<Texture2D>($"Backgrounds/Clouds_{l}");
        }
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: (lmeinen) Update position to give the illusion of moving clouds
    }

    public override void Draw(GameTime gameTime)
    {
        Matrix view = GameMain.Match.Map.Camera.View;
        Matrix projection = GameMain.Match.Map.Camera.Projection;

        for (int l = 0; l < NumLayers; l++)
        {
            // apply texture using BasicEffect
            BasicEffect effect = new(GameMain.GraphicsDevice);
            effect.Texture = this._textures[l];
            effect.TextureEnabled = true;
            effect.View = view;
            effect.Projection = projection;
            effect.Alpha = 0.8f;

            // VertexPositionTexture 
            //     Vector2 position (Viewport coordinate [-1.0->1.0]),
            //     Vector2 texturePosition (Texture coordinate [0.0->1.0]
            VertexPositionTexture[] vertices = _plane.Select(
                (point, i) => new VertexPositionTexture(
                    position: point - Vector3.UnitY * l * 0.2f * point.Y,
                    textureCoordinate: new Vector2(i / 2, i % 2)
                )
            ).ToArray();

            // apply BasicEffect
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,  // Specify tri-based quad assembly
                    vertexData: vertices,                       // Your input vertices
                    vertexOffset: 0,                            // Offset in vertex array (0 for no offset)
                    numVertices: 4,                             // Length of the input vertices array
                    indexData: new[] { 2, 1, 0, 1, 2, 3 },      // Indicies (a tri with index (0, 1, 2), and (1, 2, 3))
                    indexOffset: 0,                             // Offset in index array (0 for none)
                    primitiveCount: 2                           // Number of tris to draw (2 for a square)
                );
            }
        }
    }
}