using UnityEngine;

public enum Direction
{
    Right,
    Left,
    Up,
    Down
}

public enum BusColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Purple = 4,
    Pink = 5,
    Cyan = 6
}

public enum LevelDifficulty
{
    Normal = 0,
    Hard = 1,
    SuperHard = 2
}

public enum BoosterType
{
    SortQueue = 0,
    ShuffleColors = 1,
    VipSend = 2
}

public enum AppScreen
{
    Loading = 0,
    Home = 1,
    Shop = 2,
    Collection = 3,
    Gameplay = 4
}

public enum HubTab
{
    Shop = 0,
    Home = 1,
    Collection = 2
}

public static class DirectionExtensions
{
    public static Vector2Int ToVector2Int(this Direction dir)
    {
        return dir switch
        {
            Direction.Right => new Vector2Int(1, 0),
            Direction.Left => new Vector2Int(-1, 0),
            Direction.Up => new Vector2Int(0, 1),
            Direction.Down => new Vector2Int(0, -1),
            _ => Vector2Int.zero
        };
    }
}
