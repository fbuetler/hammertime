using System;
using Microsoft.Xna.Framework;

namespace hammertime;

public class StartOverlay : Overlay
{

    private const string texturePath = "Overlays/Start/go";

    public StartOverlay(Game game) : base(game, texturePath)
    {

    }
}