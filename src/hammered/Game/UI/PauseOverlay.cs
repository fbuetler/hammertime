using Microsoft.Xna.Framework;

namespace hammered;

public class PauseOverlay : Overlay
{

    private const string texturePath = "Overlays/Winner/";

    public PauseOverlay(Game game) : base(game, texturePath)
    {

    }
}