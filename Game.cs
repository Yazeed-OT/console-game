using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ConsoleGame;

public sealed class Game
{
    private const int PlayfieldWidth = 30;
    private const int PlayfieldHeight = 18;
    private const int InitialSnakeLength = 4;
    private const int ScorePerFood = 10;
    private const double BaseMovementIntervalMs = 180.0;
    private const double MinimumMovementIntervalMs = 60.0;
    private const double MovementIntervalStepMs = 12.0;

    private readonly Snake _snake = new();
    private readonly Random _random = new();
    private readonly Stopwatch _stopwatch = new();

    private GameState _state = GameState.Start;
    private bool _isRunning = true;

    private Position _food;
    private int _score;
    private int _level = 1;
    private int _foodsConsumed;
    private double _movementAccumulator;
    private double _movementInterval = BaseMovementIntervalMs;

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
        var willEatFood = nextHead == _food;
        var head = _snake.Move(willEatFood);

        if (IsOutOfBounds(head) || _snake.HasSelfCollision())
        {
            _state = GameState.GameOver;
            return;
        }

        if (willEatFood)
        {
            _score += ScorePerFood;
            _foodsConsumed++;
            UpdateLevelAndSpeed();
            SpawnFood();
        }
    }

    private void HandleInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true).Key;

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
        var scoreboardLine = BuildScoreboard(widthWithBorder);

        Console.SetCursorPosition(0, 0);

        // Draw scoreboard (use default color)
        Console.ResetColor();
        Console.WriteLine(scoreboardLine);

        for (var y = 0; y < heightWithBorder; y++)
        {
            for (var x = 0; x < widthWithBorder; x++)
            {
                char ch;
                ConsoleColor? color = null;

                if (y == 0 || y == heightWithBorder - 1 || x == 0 || x == widthWithBorder - 1)
                {
                    ch = '#';
                }
                else
                {
                    var pos = new Position(x, y);
                    if (_food == pos && _state != GameState.GameOver)
                    {
                        ch = 'A'; // represent apple
                        color = ConsoleColor.Red;
                    }
                    else if (_snake.TryGetSegmentAt(pos, out var index))
                    {
                        ch = index == 0 ? '@' : 'o';
                        color = ConsoleColor.Green;
                    }
                    else
                    {
                        ch = ' ';
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

                Console.Write(ch);
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        // Append message lines under the board
        var messages = new StringBuilder();
        AppendMessageLines(messages, widthWithBorder);
        Console.Write(messages.ToString());
    }

    private void AppendMessageLines(StringBuilder builder, int width)
    {
        var messageWidth = width;
        var (line1, line2, line3) = _state switch
        {
            GameState.Start => (
                "Press any direction to begin.".PadRight(messageWidth),
                "Controls: Arrow keys or WASD.".PadRight(messageWidth),
                string.Empty.PadRight(messageWidth)
            ),
            GameState.GameOver => (
                "GAME OVER".PadRight(messageWidth),
                $"Final Score: {_score}".PadRight(messageWidth),
                "Press R to restart or Q to quit.".PadRight(messageWidth)
            ),
            _ => (
                string.Empty.PadRight(messageWidth),
                string.Empty.PadRight(messageWidth),
                string.Empty.PadRight(messageWidth)
            )
        };

        builder.AppendLine(line1);
        builder.AppendLine(line2);
        builder.AppendLine(line3);
    }

    private string BuildScoreboard(int width)
    {
        var length = _snake.Length;
        var line = $"Score: {_score,4}  Level: {_level,2}  Length: {length,3}";
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
        Console.OutputEncoding = Encoding.ASCII;
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
        _level = 1;
        _foodsConsumed = 0;
        _movementAccumulator = 0;
        _movementInterval = BaseMovementIntervalMs;

        var start = new Position(PlayfieldWidth / 2, PlayfieldHeight / 2);
        _snake.Reset(start, initialDirection, InitialSnakeLength);
        SpawnFood();
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

        _food = candidate;
    }

    private void UpdateLevelAndSpeed()
    {
        _level = 1 + _foodsConsumed / 5;
        var speedIncrease = (_level - 1) * MovementIntervalStepMs;
        _movementInterval = Math.Max(MinimumMovementIntervalMs, BaseMovementIntervalMs - speedIncrease);
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
