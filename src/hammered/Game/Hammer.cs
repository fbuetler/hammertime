using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace hammered;

public class Hammer : DrawableGameComponent
{
    public Hammer(Game game) : base(game)
    {
        if (game == null)
            throw new ArgumentNullException("game");
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(GameTime gameTime)
    {
    }
}