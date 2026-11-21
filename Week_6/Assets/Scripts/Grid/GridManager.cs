using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public struct ComputedLayout
    {
        public float CellSize;
        public float VehicleScaleFactor;
        public Vector3 OriginPosition;
        public float LeftLaneX;
        public float RightLaneX;
        public float BottomLaneY;
        public float TopLaneY;
        public Vector3 OccupiedCenterWorld;
    }

    [Header("Editor-Configured Bounds & Profile")]
    [SerializeField] private PlayfieldBounds playfieldBounds;
    [SerializeField] private GridLayoutProfileSO layoutProfile;

    [Header("Fallback Defaults (When Profile / LevelData Missing)")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int cols = 6;
    [SerializeField] private float cellSize = 0.95f;
    [SerializeField] private float roadOffset = 0.45f;

    private GridCell[,] _grid;
    private Vector3 _originPosition;
    private float _currentCellSize = 0.95f;
    private float _vehicleScaleFactor = 1f;
    private float _leftLaneX;
    private float _rightLaneX;
    private float _bottomLaneY;
    private float _topLaneY;
    private readonly Dictionary<Vector2Int, BusController> _impactReservations = new Dictionary<Vector2Int, BusController>();

    public int Rows => rows;
    public int Cols => cols;
    public float CellSize => _currentCellSize;
    public float CurrentCellSize => _currentCellSize;
    public float VehicleScaleFactor => _vehicleScaleFactor;
    public float RightLaneX => _rightLaneX;
    public float LeftLaneX => _leftLaneX;
    public float BottomLaneY => _bottomLaneY;
    public float TopLaneY => _topLaneY;
    public bool HasImpactReservations => _impactReservations.Count > 0;

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeGrid();
    }

    public void InitializeGrid(LevelDataSO levelData)
    {
        if (levelData == null)
        {
            InitializeGrid();
            return;
        }

        rows = Mathf.Max(2, levelData.rows);
        cols = Mathf.Max(2, levelData.cols);

        if (playfieldBounds != null && layoutProfile != null)
        {
            ComputedLayout layout = ComputeLayout(levelData, playfieldBounds.WorldRect, layoutProfile);
            ApplyLayout(layout);
            BuildCells();
        }
        else
        {
            InitializeGrid(rows, cols);
        }
    }

    public void InitializeGrid(int newRows, int newCols)
    {
        rows = Mathf.Max(2, newRows);
        cols = Mathf.Max(2, newCols);
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        _currentCellSize = Mathf.Max(0.2f, cellSize);
        float refCell = layoutProfile != null ? Mathf.Max(0.2f, layoutProfile.referenceCellSize) : 0.95f;
        _vehicleScaleFactor = _currentCellSize / refCell;

        Vector3 center = playfieldBounds != null ? playfieldBounds.Center : transform.position;
        _originPosition = center - new Vector3((cols - 1) * _currentCellSize * 0.5f, (rows - 1) * _currentCellSize * 0.5f, 0f);
        float offset = layoutProfile != null ? _currentCellSize * layoutProfile.roadCorridorRatio : roadOffset;
        _leftLaneX = center.x - (cols * _currentCellSize * 0.5f) - offset;
        _rightLaneX = center.x + (cols * _currentCellSize * 0.5f) + offset;
        _bottomLaneY = center.y - (rows * _currentCellSize * 0.5f) - offset;
        _topLaneY = center.y + (rows * _currentCellSize * 0.5f) + offset;

        BuildCells();
    }

    private void ApplyLayout(ComputedLayout layout)
    {
        _currentCellSize = layout.CellSize;
        _vehicleScaleFactor = layout.VehicleScaleFactor;
        _originPosition = layout.OriginPosition;
        _leftLaneX = layout.LeftLaneX;
        _rightLaneX = layout.RightLaneX;
        _bottomLaneY = layout.BottomLaneY;
        _topLaneY = layout.TopLaneY;
    }

    private void BuildCells()
    {
        _impactReservations.Clear();
        _grid = new GridCell[cols, rows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                _grid[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }
    }

    /// <summary>
    /// Computes responsive cell size, vehicle scale factor, centered origin, and perimeter lanes inside a Playfield rect.
    /// </summary>
    public static ComputedLayout ComputeLayout(LevelDataSO levelData, Rect playfieldRect, GridLayoutProfileSO profile)
    {
        int boardCols = Mathf.Max(2, levelData.cols);
        int boardRows = Mathf.Max(2, levelData.rows);

        int minX = 0;
        int maxX = boardCols - 1;
        int minY = 0;
        int maxY = boardRows - 1;

        if (profile.fitToOccupiedBounds && levelData.buses != null && levelData.buses.Count > 0)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            for (int i = 0; i < levelData.buses.Count; i++)
            {
                BusSpawnData bus = levelData.buses[i];
                if (bus == null) continue;
                Vector2Int step = bus.direction.ToVector2Int();
                int len = Mathf.Clamp(bus.length, 2, 3);
                for (int k = 0; k < len; k++)
                {
                    Vector2Int cell = bus.rootGridPosition + step * k;
                    if (cell.x < minX) minX = cell.x;
                    if (cell.x > maxX) maxX = cell.x;
                    if (cell.y < minY) minY = cell.y;
                    if (cell.y > maxY) maxY = cell.y;
                }
            }

            if (minX > maxX)
            {
                minX = 0;
                maxX = boardCols - 1;
                minY = 0;
                maxY = boardRows - 1;
            }
        }

        float occupiedCols = Mathf.Max(1f, (maxX - minX + 1));
        float occupiedRows = Mathf.Max(1f, (maxY - minY + 1));

        float border = profile.fitToOccupiedBounds ? Mathf.Max(0f, profile.occupiedBorderCells) * 2f : 0f;
        float effCols = occupiedCols + border + 2f * profile.roadCorridorRatio;
        float effRows = occupiedRows + border + 2f * profile.roadCorridorRatio;

        float availW = Mathf.Max(1f, playfieldRect.width - 2f * profile.playfieldPadding.x);
        float availH = Mathf.Max(1f, playfieldRect.height - 2f * profile.playfieldPadding.y);

        float maxCell = levelData.overrideMaxCellSize
            ? Mathf.Max(profile.minCellSize, levelData.customMaxCellSize)
            : Mathf.Max(profile.minCellSize, profile.maxCellSize);

        float rawCell = Mathf.Min(availW / effCols, availH / effRows);
        float finalCell = Mathf.Clamp(rawCell, profile.minCellSize, maxCell);

        float refCell = Mathf.Max(0.2f, profile.referenceCellSize);
        float scaleFactor = Mathf.Clamp(finalCell / refCell, profile.minVehicleScale, profile.maxVehicleScale);

        float centerGridX = (minX + maxX) * 0.5f;
        float centerGridY = (minY + maxY) * 0.5f;
        Vector3 centerWorld = new Vector3(playfieldRect.center.x, playfieldRect.center.y, 0f);
        Vector3 origin = centerWorld - new Vector3(centerGridX * finalCell, centerGridY * finalCell, 0f);

        float roadMargin = finalCell * profile.roadCorridorRatio;
        float halfOccW = occupiedCols * finalCell * 0.5f;
        float halfOccH = occupiedRows * finalCell * 0.5f;

        float safePad = 0.08f;
        float leftX = Mathf.Max(playfieldRect.xMin + safePad, centerWorld.x - halfOccW - roadMargin);
        float rightX = Mathf.Min(playfieldRect.xMax - safePad, centerWorld.x + halfOccW + roadMargin);
        float bottomY = Mathf.Max(playfieldRect.yMin + safePad, centerWorld.y - halfOccH - roadMargin);
        float topY = Mathf.Min(playfieldRect.yMax - safePad, centerWorld.y + halfOccH + roadMargin);

        return new ComputedLayout
        {
            CellSize = finalCell,
            VehicleScaleFactor = scaleFactor,
            OriginPosition = origin,
            LeftLaneX = leftX,
            RightLaneX = rightX,
            BottomLaneY = bottomY,
            TopLaneY = topY,
            OccupiedCenterWorld = centerWorld
        };
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return _originPosition + new Vector3(gridPosition.x * _currentCellSize, gridPosition.y * _currentCellSize, 0f);
    }

    public bool IsInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < cols && gridPos.y >= 0 && gridPos.y < rows;
    }

    public GridCell GetCell(Vector2Int gridPos)
    {
        if (!IsInsideGrid(gridPos) || _grid == null) return null;
        return _grid[gridPos.x, gridPos.y];
    }

    public bool IsCellOccupied(Vector2Int gridPos)
    {
        GridCell cell = GetCell(gridPos);
        return cell != null && cell.IsOccupied;
    }

    public BusController GetPathBlocker(Vector2Int gridPos)
    {
        GridCell cell = GetCell(gridPos);
        if (cell != null && cell.IsOccupied) return cell.CurrentBus;
        return _impactReservations.TryGetValue(gridPos, out BusController owner) ? owner : null;
    }

    public bool ReserveImpactPath(BusController source, BusController blocker)
    {
        if (source == null || blocker == null) return false;

        Vector2Int step = source.MoveDirection.ToVector2Int();
        if (step == Vector2Int.zero) return false;
        Vector2Int pos = source.GetHeadCell() + step;
        var emptyCells = new List<Vector2Int>();
        while (IsInsideGrid(pos))
        {
            BusController occupant = GetPathBlocker(pos);
            if (occupant != null)
            {
                GridCell cell = GetCell(pos);
                if (occupant != blocker || cell == null || cell.CurrentBus != blocker) return false;
                foreach (Vector2Int c in emptyCells) _impactReservations[c] = source;
                return true;
            }
            emptyCells.Add(pos);
            pos += step;
        }
        return false;
    }

    public void ReleaseImpactPath(BusController source)
    {
        var cells = new List<Vector2Int>();
        foreach (var reservation in _impactReservations)
        {
            if (reservation.Value == source) cells.Add(reservation.Key);
        }
        foreach (Vector2Int cell in cells) _impactReservations.Remove(cell);
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
            {
                cell.ClearCell();
            }
        }
    }

    private void OnDrawGizmos()
    {
        float previewCell = Application.isPlaying ? _currentCellSize : Mathf.Max(0.2f, cellSize);
        Vector3 origin = Application.isPlaying
            ? _originPosition
            : (playfieldBounds != null ? playfieldBounds.Center : transform.position) -
              new Vector3((cols - 1) * previewCell * 0.5f, (rows - 1) * previewCell * 0.5f, 0f);

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.45f);
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = origin + new Vector3(x * previewCell, y * previewCell, 0f);
                Gizmos.DrawWireCube(cellCenter, new Vector3(previewCell * 0.9f, previewCell * 0.9f, 0.05f));
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.7f);
            Vector3 bl = new Vector3(_leftLaneX, _bottomLaneY, 0f);
            Vector3 br = new Vector3(_rightLaneX, _bottomLaneY, 0f);
            Vector3 tr = new Vector3(_rightLaneX, _topLaneY, 0f);
            Vector3 tl = new Vector3(_leftLaneX, _topLaneY, 0f);
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}
