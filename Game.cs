using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleGame;

public sealed class Game
{
    private const int PlayfieldWidth = 30;
    private const int PlayfieldHeight = 18;
    private const int InitialSnakeLength = 4;
    private const double BaseMovementIntervalMs = 180.0;
    private const double MinimumMovementIntervalMs = 60.0;
    private const double MovementIntervalStepMs = 12.0;

    private readonly Snake _snake = new();
    private readonly Random _random = new();
    private readonly Stopwatch _stopwatch = new();

    private GameState _state = GameState.Start;
    private bool _isRunning = true;

    private Item _item;
    private Difficulty _difficulty = Difficulty.Normal;
    private int _score;
    private int _highScore;
    private bool _isNewRecord;
    private int _level = 1;
    private int _foodsConsumed;
    private double _movementAccumulator;
    private double _movementInterval = BaseMovementIntervalMs;

    private const string HighScoreFileName = "highscore.txt";
    private double _newRecordBlinkStartMs;
    private const double NewRecordBlinkDurationMs = 3000.0;
    private const double NewRecordBlinkIntervalMs = 500.0;
    private bool _newRecordBlinkActive;
    private bool _newRecordBlinkOn;

    private enum Difficulty
    {
        Normal = 1,
        Extreme = 2
    }

    private enum ItemType
    {
        Normal,
        Rare,
        Poison
    }

    private readonly struct Item
    {
        public Item(Position position, ItemType type)
        {
            Position = position;
            Type = type;
        }

        public Position Position { get; }
        public ItemType Type { get; }
    }

    public void Run()
    {
        PrepareConsole();
        ResetGame();

        _stopwatch.Start();
        double previousElapsed = _stopwatch.Elapsed.TotalMilliseconds;

        while (_isRunning)
        {
            var now = _stopwatch.Elapsed.TotalMilliseconds;
            var delta = now - previousElapsed;
            previousElapsed = now;

            HandleInput();

            if (_state == GameState.Running)
            {
                _movementAccumulator += delta;
                while (_movementAccumulator >= _movementInterval)
                {
                    Update();
                    _movementAccumulator -= _movementInterval;
                    if (_state != GameState.Running)
                    {
                        break;
                    }
                }
            }

            Render();
            Thread.Sleep(16);
        }

        Console.ResetColor();
        Console.Clear();
        Console.CursorVisible = true;
    }

    private void Update()
    {
        var nextHead = _snake.PeekNextHead();
        var itemAtNext = _item.Position == nextHead ? _item : default;
        var willEat = _item.Position == nextHead;

        // Determine growth based on item type (poison does not grow)
        var grow = willEat && _item.Type != ItemType.Poison;
        var head = _snake.Move(grow);

        if (IsOutOfBounds(head) || _snake.HasSelfCollision())
        {
            _state = GameState.GameOver;
            // Check for new record (score at time of death)
            if (_score > _highScore)
            {
                _highScore = _score;
                SaveHighScore();
                _isNewRecord = true;
                // start blink timer
                _newRecordBlinkStartMs = _stopwatch.Elapsed.TotalMilliseconds;
            }
            else
            {
                _isNewRecord = false;
                _newRecordBlinkStartMs = 0;
            }

            return;
        }

        if (willEat)
        {
            switch (_item.Type)
            {
                case ItemType.Normal:
                    // Normal apples are worth 10 points in Normal mode and in Extreme mode
                    _score += 10;
                    _foodsConsumed++;
                    break;
                case ItemType.Rare:
                    _score += _difficulty == Difficulty.Extreme ? 20 : 8;
                    _foodsConsumed++;
                    break;
                case ItemType.Poison:
                    // Poison is -10 on all difficulties (as requested)
                    _score = Math.Max(0, _score - 10);
                    break;
            }

            UpdateLevelAndSpeed();
            SpawnFood();
        }
    }

    private void HandleInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true).Key;

            // Allow mode selection on the start screen with keys 1-2
            if (_state == GameState.Start)
            {
                switch (key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        _difficulty = Difficulty.Normal;
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        _difficulty = Difficulty.Extreme;
                        break;
                }
            }

            if (TryMapToDirection(key, out var direction))
            {
                if (_state == GameState.Start)
                {
                    StartGame(direction);
                }

                if (_state == GameState.Running)
                {
                    _snake.QueueDirection(direction);
                }

                continue;
            }

            switch (key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    if (_state == GameState.Start)
                    {
                        StartGame(Direction.Right);
                    }
                    break;
                case ConsoleKey.R:
                    if (_state == GameState.GameOver)
                    {
                        StartGame(Direction.Right);
                    }
                    break;
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    _isRunning = false;
                    break;
            }
        }
    }

    private void Render()
    {
        var widthWithBorder = PlayfieldWidth + 2;
        var heightWithBorder = PlayfieldHeight + 2;
        var isExtreme = _difficulty == Difficulty.Extreme;
        // In Extreme mode inner cells are rendered as two characters wide, so the displayed
        // line width increases by PlayfieldWidth characters.
        var displayedWidth = isExtreme ? widthWithBorder + PlayfieldWidth : widthWithBorder;
        // The maximum possible displayed width (when Extreme is active) — use this to fully
        // clear lines so switching between modes doesn't leave leftover characters.
        var maxDisplayedWidth = widthWithBorder + PlayfieldWidth;
        var scoreboardLine = BuildScoreboard(maxDisplayedWidth);

        Console.SetCursorPosition(0, 0);

        // Compute blink state once so scoreboard and messages stay in sync
        var elapsedSinceNewRecord = _newRecordBlinkStartMs > 0 ? _stopwatch.Elapsed.TotalMilliseconds - _newRecordBlinkStartMs : 0.0;
        _newRecordBlinkActive = _isNewRecord && _newRecordBlinkStartMs > 0 && elapsedSinceNewRecord <= NewRecordBlinkDurationMs;
        _newRecordBlinkOn = _newRecordBlinkActive && (((int)(elapsedSinceNewRecord / NewRecordBlinkIntervalMs) % 2) == 0);

        // Draw scoreboard with colored high score (blink when new record)
        Console.ResetColor();
        var scorePart = $"Score: {_score,4}  ";
        var highPart = $"High: {_highScore,4}  ";
        var restPart = $"Level: {_level,2}  Length: {_snake.Length,3}";

        // Write the scoreboard piecewise but ensure the full line is overwritten
        Console.Write(scorePart);
        if (_newRecordBlinkActive)
        {
            if (_newRecordBlinkOn)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(highPart);
                Console.ResetColor();
            }
            else
            {
                Console.Write(new string(' ', highPart.Length));
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(highPart);
            Console.ResetColor();
        }

        // Compose the rest of the scoreboard and pad to the maximum displayed width to avoid leftover text
        Console.Write(restPart);
        var currentLine = scorePart + ( _newRecordBlinkActive && !_newRecordBlinkOn ? new string(' ', highPart.Length) : highPart ) + restPart;
        Console.Write(new string(' ', Math.Max(0, maxDisplayedWidth - currentLine.Length)));
        Console.WriteLine();

        // Border characters (box-drawing)
        const string topLeft = "╔";
        const string topRight = "╗";
        const string bottomLeft = "╚";
        const string bottomRight = "╝";
        const string horizontal = "═";
        const string vertical = "║";

        var borderColor = ConsoleColor.Cyan;

        for (var y = 0; y < heightWithBorder; y++)
        {
            for (var x = 0; x < widthWithBorder; x++)
            {
                string cell = " ";
                ConsoleColor? color = null;

                // Border
                if (y == 0)
                {
                    if (x == 0) cell = topLeft;
                    else if (x == widthWithBorder - 1) cell = topRight;
                    else cell = horizontal;
                    color = borderColor;
                }
                else if (y == heightWithBorder - 1)
                {
                    if (x == 0) cell = bottomLeft;
                    else if (x == widthWithBorder - 1) cell = bottomRight;
                    else cell = horizontal;
                    color = borderColor;
                }
                else if (x == 0 || x == widthWithBorder - 1)
                {
                    cell = vertical;
                    color = borderColor;
                }
                else
                {
                    var pos = new Position(x, y);
                    if (_item.Position == pos && _state != GameState.GameOver)
                    {
                        // Render item based on type and difficulty
                        switch (_item.Type)
                        {
                            case ItemType.Normal:
                                cell = _difficulty == Difficulty.Extreme ? "🍎" : "A";
                                color = ConsoleColor.Red;
                                break;
                            case ItemType.Rare:
                                cell = _difficulty == Difficulty.Extreme ? "🍇" : "G";
                                color = ConsoleColor.Magenta;
                                break;
                            case ItemType.Poison:
                                cell = _difficulty == Difficulty.Extreme ? "💣" : "X";
                                color = ConsoleColor.DarkRed;
                                break;
                        }
                    }
                    else if (_snake.TryGetSegmentAt(pos, out var index))
                    {
                        // Make the snake look a bit more "alive":
                        // - Use a distinct head with eyes
                        // - Draw the tail with a smaller dot
                        // - Keep the body rounded
                        if (index == 0)
                        {
                            // Use a bold circle for the head so it
                            // stands out from the body segments.
                            cell = "●";
                            color = ConsoleColor.Green;
                        }
                        else if (index == _snake.Length - 1)
                        {
                            cell = "·"; // tail
                            color = ConsoleColor.DarkGreen;
                        }
                        else
                        {
                            cell = "○"; // body
                            color = ConsoleColor.Green;
                        }
                    }
                    else
                    {
                        cell = " ";
                    }
                }

                if (color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                }
                else
                {
                    Console.ResetColor();
                }

                // In Extreme mode we render cells as roughly two columns wide.
                // Emoji items are printed as-is (they usually occupy two columns).
                // Other cells get padded with one extra space so the grid stays aligned.
                if (isExtreme)
                {
                    if (cell == "🍎" || cell == "🍇" || cell == "💣")
                    {
                        Console.Write(cell);
                    }
                    else if (cell == horizontal)
                    {
                        // draw horizontal double for nicer border in extreme mode
                        Console.Write(new string('═', 2));
                    }
                    else
                    {
                        Console.Write(cell);
                        Console.Write(' ');
                    }
                }
                else
                {
                    Console.Write(cell);
                }
            }

            // After finishing the board line, pad to the maximum displayed width so any
            // leftover characters from a previous frame (e.g. when switching from Extreme)
            // are cleared, then end the line and reset colors.
            var padLength = Math.Max(0, maxDisplayedWidth - displayedWidth);
            if (padLength > 0)
            {
                Console.Write(new string(' ', padLength));
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        // Append message lines under the board
        var messages = new StringBuilder();
        // Use maxDisplayedWidth so message lines fully cover any previous content
        AppendMessageLines(messages, maxDisplayedWidth);
        Console.Write(messages.ToString());
    }

    private void AppendMessageLines(StringBuilder builder, int width)
    {
        var messageWidth = width;
        // We'll produce up to 4 message lines so the restart/quit line sits below the NEW RECORD line
        string line1 = string.Empty.PadRight(messageWidth);
        string line2 = string.Empty.PadRight(messageWidth);
        string line3 = string.Empty.PadRight(messageWidth); // NEW RECORD (may blink)
        string line4 = string.Empty.PadRight(messageWidth); // persistent restart/quit

        if (_state == GameState.Start)
        {
            line1 = "Select Mode: 1. Normal   2. Extreme".PadRight(messageWidth);
            line2 = ($"Selected: {_difficulty}    Press any direction to begin.").PadRight(messageWidth);
        }
        else if (_state == GameState.GameOver)
        {
            line1 = "GAME OVER".PadRight(messageWidth);
            line2 = $"Final Score: {_score}".PadRight(messageWidth);
            line3 = _isNewRecord ? "NEW RECORD!".PadRight(messageWidth) : string.Empty.PadRight(messageWidth);
            line4 = "Press R to restart or Q to quit.".PadRight(messageWidth);
        }

        // Append line1 and line2 always
        builder.AppendLine(line1);
        builder.AppendLine(line2);

        // If there's a blinking NEW RECORD line, render it using the shared blink state
        if (_state == GameState.GameOver && _isNewRecord && _newRecordBlinkActive)
        {
            if (_newRecordBlinkOn)
            {
                builder.AppendLine(line3);
            }
            else
            {
                builder.AppendLine(new string(' ', messageWidth));
            }
        }
        else
        {
            builder.AppendLine(line3);
        }

        // Append the persistent restart/quit line (does not blink)
        builder.AppendLine(line4);
    }

    private string BuildScoreboard(int width)
    {
        var length = _snake.Length;
        var line = $"Score: {_score,4}  High: {_highScore,4}  Level: {_level,2}  Length: {length,3}";
        return line.PadRight(width);
    }

    private static bool TryMapToDirection(ConsoleKey key, out Direction direction)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                direction = Direction.Up;
                return true;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                direction = Direction.Down;
                return true;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                direction = Direction.Left;
                return true;
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                direction = Direction.Right;
                return true;
            default:
                direction = default;
                return false;
        }
    }

    private void PrepareConsole()
    {
        // Use UTF8 so box-drawing characters render in most terminals
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Clear();
        var requiredWidth = PlayfieldWidth + 4;
        var requiredHeight = PlayfieldHeight + 8;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (Console.WindowWidth < requiredWidth || Console.WindowHeight < requiredHeight)
                {
                    Console.SetWindowSize(Math.Max(Console.WindowWidth, requiredWidth), Math.Max(Console.WindowHeight, requiredHeight));
                }

                if (Console.BufferWidth < requiredWidth || Console.BufferHeight < requiredHeight)
                {
                    Console.SetBufferSize(Math.Max(Console.BufferWidth, requiredWidth), Math.Max(Console.BufferHeight, requiredHeight));
                }
            }
            catch
            {
                // Some terminals disallow resizing; ignore and proceed with current dimensions.
            }
        }
    }

    private void StartGame(Direction initialDirection)
    {
        ResetGame(initialDirection);
        _state = GameState.Running;
    }

    private void ResetGame()
    {
        ResetGame(Direction.Right);
    }

    private void ResetGame(Direction initialDirection)
    {
        _score = 0;
        _isNewRecord = false;
        LoadHighScore();
        _level = 1;
        _foodsConsumed = 0;
        _movementAccumulator = 0;
        // Adjust base speed based on difficulty
        var difficultyMultiplier = _difficulty switch
        {
            Difficulty.Normal => 1.0,
            Difficulty.Extreme => 0.8,
            _ => 1.0
        };

        _movementInterval = BaseMovementIntervalMs * difficultyMultiplier;

        var start = new Position(PlayfieldWidth / 2, PlayfieldHeight / 2);
        _snake.Reset(start, initialDirection, InitialSnakeLength);
        SpawnFood();
    }

    private void LoadHighScore()
    {
        try
        {
            if (File.Exists(HighScoreFileName))
            {
                var text = File.ReadAllText(HighScoreFileName).Trim();
                if (int.TryParse(text, out var parsed))
                {
                    _highScore = parsed;
                    return;
                }
            }
        }
        catch
        {
            // Ignore failures reading the high score file.
        }

        _highScore = 0;
    }

    private void SaveHighScore()
    {
        try
        {
            File.WriteAllText(HighScoreFileName, _highScore.ToString());
        }
        catch
        {
            // Ignore failures writing the high score file.
        }
    }

    private void SpawnFood()
    {
        Position candidate;
        do
        {
            var x = _random.Next(1, PlayfieldWidth + 1);
            var y = _random.Next(1, PlayfieldHeight + 1);
            candidate = new Position(x, y);
        } while (_snake.Contains(candidate));

        // Determine item type based on difficulty
        ItemType type = ItemType.Normal;
        if (_difficulty == Difficulty.Extreme)
        {
            // Extreme: Normal ~70%, Rare ~20%, Poison ~10%
            var roll = _random.Next(100);
            if (roll < 70) type = ItemType.Normal;
            else if (roll < 90) type = ItemType.Rare;
            else type = ItemType.Poison;
        }
        else
        {
            // Normal: always normal
            type = ItemType.Normal;
        }

        _item = new Item(candidate, type);
    }

    private void UpdateLevelAndSpeed()
    {
        _level = 1 + _foodsConsumed / 5;
        var speedIncrease = (_level - 1) * MovementIntervalStepMs;
        // apply level speed change on top of difficulty-adjusted base interval
        _movementInterval = Math.Max(MinimumMovementIntervalMs, _movementInterval - speedIncrease);
    }

    private static bool IsOutOfBounds(Position position)
    {
        return position.X <= 0 || position.X >= PlayfieldWidth + 1 || position.Y <= 0 || position.Y >= PlayfieldHeight + 1;
    }
}

public enum GameState
{
    Start,
    Running,
    GameOver
}
