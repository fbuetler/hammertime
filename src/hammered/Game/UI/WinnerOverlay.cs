using Microsoft.Xna.Framework;

namespace hammered;

public class WinnerOverlay : Overlay
{

    private const string texturePathPrefix = "Overlays/Winner/";

    public WinnerOverlay(Game game, string type) : base(game, $"{texturePathPrefix}{type}")
    {

    }
}

