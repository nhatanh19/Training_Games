using UnityEngine;

public class GridCell
{
    public Vector2Int GridPosition { get; private set; }

    public BusController CurrentBus { get; private set; }

    public bool IsOccupied => CurrentBus != null;

    public GridCell(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        CurrentBus = null;
    }

    public void SetOccupied(BusController bus)
    {
        CurrentBus = bus;
    }
    
    public void ClearCell()
    {
        CurrentBus = null;
    }
}
