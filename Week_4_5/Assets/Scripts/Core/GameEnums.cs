using UnityEngine;

public enum Direction
{
    Right,
    Left,
    Up,
    Down
}

public enum BusColor{
    Red,
    Blue,
    Green,
    Yellow
}

public static class DirectionExtensions{
    public static Vector2Int ToVector2Int(this Direction dir){
        return dir switch{
            Direction.Right => new Vector2Int(1,0),
            Direction.Left => new Vector2Int(-1,0),
            Direction.Up => new Vector2Int(0,1),
            Direction.Down => new Vector2Int(0,-1),
            _ => Vector2Int.zero
        };
    }
}