
using Apos.Input;
using Microsoft.Xna.Framework.Input;

namespace hammered;

public static class Controls
{
    public static int PlayerIndex = 0;

    public static ICondition Start { get; set; } =
            new AnyCondition(
                new KeyboardCondition(Keys.Space),
                new KeyboardCondition(Keys.Enter),
                new GamePadCondition(GamePadButton.A, PlayerIndex),
                new GamePadCondition(GamePadButton.Start, PlayerIndex)
            );

    public static ICondition Back { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Escape),
            new GamePadCondition(GamePadButton.B, PlayerIndex),
            new GamePadCondition(GamePadButton.Back, PlayerIndex)
        );

    public static ICondition Interact { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Space),
            new KeyboardCondition(Keys.Enter),
            new GamePadCondition(GamePadButton.A, PlayerIndex)
        );

    public static ICondition FocusPrev { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Up),
            new AllCondition(
                new AnyCondition(
                    new KeyboardCondition(Keys.LeftShift),
                    new KeyboardCondition(Keys.RightShift)
                ),
                new KeyboardCondition(Keys.Tab)
            ),
            new GamePadCondition(GamePadButton.Up, PlayerIndex)
        );

    public static ICondition FocusNext { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Down),
            new KeyboardCondition(Keys.Tab),
            new GamePadCondition(GamePadButton.Down, PlayerIndex)
        );

    public static ICondition MoveLeft { get; set; } =
    new AnyCondition(
        new KeyboardCondition(Keys.Left),
        new GamePadCondition(GamePadButton.Left, PlayerIndex)
    );

    public static ICondition MoveRight { get; set; } =
        new AnyCondition(
            new KeyboardCondition(Keys.Right),
            new GamePadCondition(GamePadButton.Right, PlayerIndex)
        );
}