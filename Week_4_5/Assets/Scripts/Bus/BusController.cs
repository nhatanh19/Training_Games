using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Header("Bus Properties")]
    [SerializeField] private BusColor busColor;
    [SerializeField] private Direction direction;
    [Range(2, 3)]
    [SerializeField] private int length = 2;
    [SerializeField] private Vector2Int rootGridPosition;

    [Header("Seats (Must be 3 points)")]
    [SerializeField] private List<Transform> seatPoints = new List<Transform>();

    private GridManager _gridManager;
    private bool _isBusy = false;

    public BusColor Color => busColor;
    public Direction MoveDirection => direction;
    public int Length => length;
    public Vector2Int RootGridPosition => rootGridPosition;
    public List<Transform> SeatPoints => seatPoints;

    private void OnValidate()
    {
        ApplyColor();
    }

    private void Start()
    {
        _gridManager = FindObjectOfType<GridManager>();
        ApplyColor();
        AlignToGrid();
        RegisterToGrid();
    }

    public void ApplyColor()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.color = GetVisualColor(busColor);
        }
    }

    private UnityEngine.Color GetVisualColor(BusColor color)
    {
        return color switch
        {
            BusColor.Red => new UnityEngine.Color(0.9f, 0.25f, 0.25f),
            BusColor.Blue => new UnityEngine.Color(0.2f, 0.5f, 0.95f),
            BusColor.Green => new UnityEngine.Color(0.25f, 0.85f, 0.35f),
            BusColor.Yellow => new UnityEngine.Color(0.95f, 0.85f, 0.15f),
            _ => UnityEngine.Color.white
        };
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int step = direction.ToVector2Int();

        for (int i = 0; i < length; i++)
        {
            cells.Add(rootGridPosition + step * i);
        }

        return cells;
    }

    public Vector2Int GetHeadCell()
    {
        return rootGridPosition + direction.ToVector2Int() * (length - 1);
    }

    public void AlignToGrid()
    {
        if (_gridManager == null) return;

        Vector2Int tailPos = rootGridPosition;
        Vector2Int headPos = GetHeadCell();

        Vector3 tailWorld = _gridManager.GridToWorldPosition(tailPos);
        Vector3 headWorld = _gridManager.GridToWorldPosition(headPos);

        transform.position = (tailWorld + headWorld) * 0.5f;

        float angle = direction switch
        {
            Direction.Right => 0f,
            Direction.Up => 90f,
            Direction.Left => 180f,
            Direction.Down => 270f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void RegisterToGrid()
    {
        if (_gridManager != null)
        {
            _gridManager.RegisterBus(this, GetOccupiedCells());
        }
    }

    public bool CanExit()
    {
        if (_gridManager == null) return false;

        Vector2Int step = direction.ToVector2Int();
        Vector2Int checkPos = GetHeadCell() + step;

        while (_gridManager.IsInsideGrid(checkPos))
        {
            if (_gridManager.IsCellOccupied(checkPos))
            {
                return false;
            }
            checkPos += step;
        }

        return true;
    }

    private void OnMouseDown()
    {
        if (_isBusy) return;

        if (CanExit())
        {
            Debug.Log($"Xe {busColor} thông đường, sẵn sàng xuất bến!");
        }
        else
        {
            PlayBlockedFeedback();
        }
    }

    private void PlayBlockedFeedback()
    {
        _isBusy = true;
        Vector2Int step = direction.ToVector2Int();
        Vector3 punchVector = new Vector3(step.x, step.y, 0f) * 0.2f;

        transform.DOPunchPosition(punchVector, 0.25f, 8, 0.5f).OnComplete(() =>
        {
            _isBusy = false;
        });
    }
}
