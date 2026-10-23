using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int cols = 6;
    [SerializeField] private float cellSize = 0.95f;

    [Header("Road Corridor Settings")]
    [SerializeField] private float roadOffset = 0.65f;

    private GridCell[,] _grid;
    private Vector3 _originPosition;

    public int Rows => rows;
    public int Cols => cols;
    public float CellSize => cellSize;
    public float RightLaneX => transform.position.x + (cols * cellSize * 0.5f) + roadOffset;
    public float LeftLaneX => transform.position.x - (cols * cellSize * 0.5f) - roadOffset;
    public float BottomLaneY => transform.position.y - (rows * cellSize * 0.5f) - roadOffset;
    public float TopLaneY => transform.position.y + (rows * cellSize * 0.5f) + roadOffset;

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (transform.position.y < -0.8f)
        {
            transform.position = new Vector3(transform.position.x, -0.6f, transform.position.z);
        }

        if (Camera.main != null && Camera.main.orthographic)
        {
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 7.8f);
        }

        InitializeGrid();
    }

    public void InitializeGrid(int newRows, int newCols)
    {
        rows = newRows;
        cols = newCols;
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        _grid = new GridCell[cols, rows];
        _originPosition = transform.position - new Vector3((cols - 1) * cellSize * 0.5f, (rows - 1) * cellSize * 0.5f, 0f);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                _grid[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return _originPosition + new Vector3(gridPosition.x * cellSize, gridPosition.y * cellSize, 0f);
    }

    public bool IsInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < cols && gridPos.y >= 0 && gridPos.y < rows;
    }

    public GridCell GetCell(Vector2Int gridPos)
    {
        if (!IsInsideGrid(gridPos)) return null;
        return _grid[gridPos.x, gridPos.y];
    }

    public bool IsCellOccupied(Vector2Int gridPos)
    {
        GridCell cell = GetCell(gridPos);
        return cell != null && cell.IsOccupied;
    }

    public void RegisterBus(BusController bus, List<Vector2Int> occupiedCells)
    {
        foreach (var pos in occupiedCells)
        {
            GridCell cell = GetCell(pos);
            if (cell != null)
            {
                cell.SetOccupied(bus);
            }
        }
    }

    public void UnregisterBus(List<Vector2Int> occupiedCells)
    {
        foreach (var pos in occupiedCells)
        {
            GridCell cell = GetCell(pos);
            if (cell != null)
            {                cell.ClearCell();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position - new Vector3((cols - 1) * cellSize * 0.5f, (rows - 1) * cellSize * 0.5f, 0f);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = origin + new Vector3(x * cellSize, y * cellSize, 0f);
                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));
            }
        }
    }
}