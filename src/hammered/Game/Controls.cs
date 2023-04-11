
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

    public static ICondition Interact { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, PrimaryPlayerIndex)
        );

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
            new KeyboardCondition(Keys.R),
            new GamePadCondition(GamePadButton.X, PrimaryPlayerIndex)
        );

    public static ICondition NextMap { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.N),
            new GamePadCondition(GamePadButton.Y, PrimaryPlayerIndex)
        );

    public static ICondition Pause { get; } =
        new AnyCondition(
            new KeyboardCondition(Keys.P),
            new GamePadCondition(GamePadButton.Start, PrimaryPlayerIndex)
        );

    public static ICondition Throw(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new GamePadCondition(GamePadButton.RightShoulder, playerIndex)
        );
    }

    public static ICondition MoveUp(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Up),
            new GamePadCondition(GamePadButton.Up, playerIndex)
        );
    }

    public static ICondition MoveDown(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Down),
            new GamePadCondition(GamePadButton.Down, playerIndex)
        );
    }

    public static ICondition MoveLeft(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Left),
            new GamePadCondition(GamePadButton.Left, playerIndex)
        );
    }

    public static ICondition MoveRight(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.Right),
            new GamePadCondition(GamePadButton.Right, playerIndex)
        );
    }

    public static Vector2 Move(int playerIndex)
    {
        return InputHelper.NewGamePad[playerIndex].ThumbSticks.Left * MoveStickScale;
    }

    public static ICondition AimUp(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.W)
        );
    }

    public static ICondition AimDown(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.S)
        );
    }

    public static ICondition AimLeft(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.A)
        );
    }

    public static ICondition AimRight(int playerIndex)
    {
        return new AnyCondition(
            new KeyboardCondition(Keys.D)
        );
    }

    public static Vector2 Aim(int playerIndex)
    {
        return InputHelper.NewGamePad[playerIndex].ThumbSticks.Right * AimStickScale;
    }
}