using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace hammertime;

public abstract class Overlay : DrawableGameComponent
{
    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private string _texturePath;

    private Texture2D _texture;

    public Overlay(Game game, string texturePath) : base(game)
    {
        _game = (GameMain)game;

        _texturePath = texturePath;

        // make update and draw called by monogame
        Enabled = false;
        Visible = true;
        DrawOrder = GameMain.OVERLAY_DRAW_ORDER;
    }

    protected override void LoadContent()
    {
        _texture = GameMain.Content.Load<Texture2D>(_texturePath);
    }

    public override void Draw(GameTime gameTime)
    {
        // _spriteBatch.Begin alters the state of the graphics pipeline
        // therefore we have to reenable the depth buffer here
        GameMain.SpriteBatch.Begin(depthStencilState: DepthStencilState.Default);
        DrawFullScreen(GameMain.SpriteBatch, _texture);
        GameMain.SpriteBatch.End();

        base.Draw(gameTime);
    }

    protected void DrawFullScreen(SpriteBatch spriteBatch, Texture2D texture)
    {
        Vector2 scale = new Vector2(
                    GameMain.GetScreenWidth() / (float)texture.Width,
                    GameMain.GetScreenHeight() / (float)texture.Height
                );
        spriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}