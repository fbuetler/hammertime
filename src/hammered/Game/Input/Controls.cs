
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public static class Controls
{
    // TODO (fbuetler) proper input handling instead of just taking the input of the first player
    public static int PrimaryPlayerIndex = 0;

    private const float MoveStickScale = 1.0f;
    private const float AimStickScale = 1.0f;

    public static ICondition Start { get; } =
            new AnyCondition(
                new KeyboardCondition(Keys.Space),
                new KeyboardCondition(Keys.Enter),
                new GamePadCondition(GamePadButton.A, PrimaryPlayerIndex),
                new GamePadCondition(GamePadButton.Start, PrimaryPlayerIndex)
            );

    public static ICondition Back { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Escape),
            new GamePadCondition(GamePadButton.B, PrimaryPlayerIndex),
            new GamePadCondition(GamePadButton.Back, PrimaryPlayerIndex)
        );

    public static ICondition BackP(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Escape),
            new GamePadCondition(GamePadButton.B, playerIndex),
            new GamePadCondition(GamePadButton.Back, playerIndex)
        );
    }

    public static ICondition Interact { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, PrimaryPlayerIndex)
        );

    public static ICondition InteractP(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, playerIndex)
        );
    }

    public static ICondition FocusPrev { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Up),
            new AllCondition(
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftShift),
                    new KeyboardCondition(Keys.RightShift)
                ),
                new KeyboardCondition(Keys.Tab)
            ),
            new GamePadCondition(GamePadButton.Up, PrimaryPlayerIndex)
        );

    public static ICondition FocusNext { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Down),
            new KeyboardCondition(Keys.Tab),
            new GamePadCondition(GamePadButton.Down, PrimaryPlayerIndex)
        );

    public static ICondition Decrease { get; } =
    new AnyCondition(
        new KeyboardCondition(Keys.Left),
        new GamePadCondition(GamePadButton.Left, PrimaryPlayerIndex)
    );

    public static ICondition Increase { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Right),
            new GamePadCondition(GamePadButton.Right, PrimaryPlayerIndex)
        );

    public static ICondition ReloadMap { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.R)
        );

    public static ICondition NextMap { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.N)
        );

    public static ICondition Pause { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.P),
            new GamePadCondition(GamePadButton.Start, PrimaryPlayerIndex)
        );

    public static ICondition Throw(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.Space),
                new GamePadCondition(GamePadButton.RightShoulder, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.RightShoulder, playerIndex)
        );
    }

    public static ICondition Dash(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.LeftShift),
                new GamePadCondition(GamePadButton.LeftShoulder, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.LeftShoulder, playerIndex)
        );
    }

    public static ICondition MoveUp(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.Up),
                new GamePadCondition(GamePadButton.Up, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.Up, playerIndex)
        );
    }

    public static ICondition MoveDown(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.Down),
                new GamePadCondition(GamePadButton.Down, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.Down, playerIndex)
        );
    }

    public static ICondition MoveLeft(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.Left),
                new GamePadCondition(GamePadButton.Left, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.Left, playerIndex)
        );
    }

    public static ICondition MoveRight(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.Right),
                new GamePadCondition(GamePadButton.Right, playerIndex)
            );
        return new AnyCondition(
            new GamePadCondition(GamePadButton.Right, playerIndex)
        );
    }

    public static Vector2 Move(int playerIndex)
    {
        return InputHelper.NewGamePad[playerIndex].ThumbSticks.Left * MoveStickScale;
    }

    public static ICondition AimUp(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.W)
            );
        return new AnyCondition();
    }

    public static ICondition AimDown(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.S)
            );
        return new AnyCondition();
    }

    public static ICondition AimLeft(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.A)
            );
        return new AnyCondition();
    }

    public static ICondition AimRight(int playerIndex)
    {
        if (playerIndex == 0)
            return new AnyCondition(
                new KeyboardCondition(Keys.D)
            );
        return new AnyCondition();
    }

    public static Vector2 Aim(int playerIndex)
    {
        return InputHelper.NewGamePad[playerIndex].ThumbSticks.Right * AimStickScale;
    }

    public static int ConnectedPlayers()
    {
        int connected = 0;
        for (int i = 0; i < Match.MaxNumberOfPlayers; i++)
        {
            if (InputHelper.NewGamePad[i].IsConnected)
            {
                connected++;
            }
        }
        return connected;
    }
}