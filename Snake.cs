using System;
using System.Collections.Generic;

namespace ConsoleGame;

public sealed class Snake
{
    private readonly List<Position> _segments = new();
    private Direction _currentDirection = Direction.Right;
    private Direction _nextDirection = Direction.Right;

    public Direction CurrentDirection => _currentDirection;
    public int Length => _segments.Count;

    public void Reset(Position start, Direction direction, int length)
    {
        _segments.Clear();
        _currentDirection = direction;
        _nextDirection = direction;

        _segments.Add(start);
        var delta = DirectionToOffset(direction);

        for (var i = 1; i < length; i++)
        {
            var segment = new Position(start.X - delta.X * i, start.Y - delta.Y * i);
            _segments.Add(segment);
        }
    }

    public void QueueDirection(Direction direction)
    {
        if (direction == _nextDirection || direction == _currentDirection)
        {
            return;
        }

        if (IsOpposite(direction, _currentDirection) || IsOpposite(direction, _nextDirection))
        {
            return;
        }

        _nextDirection = direction;
    }

    public Position Move(bool grow)
    {
        _currentDirection = _nextDirection;
        var head = _segments[0];
        var movement = DirectionToOffset(_currentDirection);
        var newHead = new Position(head.X + movement.X, head.Y + movement.Y);
        _segments.Insert(0, newHead);

        if (!grow)
        {
            _segments.RemoveAt(_segments.Count - 1);
        }

        return newHead;
    }

    public bool HasSelfCollision()
    {
        if (_segments.Count < 5)
        {
            return false;
        }

        var head = _segments[0];
        for (var i = 1; i < _segments.Count; i++)
        {
            if (_segments[i] == head)
            {
                return true;
            }
        }

        return false;
    }

    public bool Contains(Position position)
    {
        return _segments.Contains(position);
    }

    public Position PeekNextHead()
    {
        if (_segments.Count == 0)
        {
            return default;
        }

        var head = _segments[0];
        var movement = DirectionToOffset(_nextDirection);
        return new Position(head.X + movement.X, head.Y + movement.Y);
    }

    public bool TryGetSegmentAt(Position position, out int index)
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] == position)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static bool IsOpposite(Direction a, Direction b)
    {
        return (a, b) switch
        {
            (Direction.Up, Direction.Down) => true,
            (Direction.Down, Direction.Up) => true,
            (Direction.Left, Direction.Right) => true,
            (Direction.Right, Direction.Left) => true,
            _ => false
        };
    }

    private static (int X, int Y) DirectionToOffset(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };
    }
}
