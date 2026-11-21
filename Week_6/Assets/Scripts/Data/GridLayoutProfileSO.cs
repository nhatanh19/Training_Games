using UnityEngine;

[CreateAssetMenu(fileName = "GridLayoutProfile", menuName = "BusJam/Data/Grid Layout Profile")]
public class GridLayoutProfileSO : ScriptableObject
{
    [Header("Cell Auto-Scaling Bounds")]
    [Tooltip("Minimum grid cell size when the board has many vehicles.")]
    [Min(0.2f)] public float minCellSize = 0.62f;

    [Tooltip("Maximum grid cell size when the board has few vehicles.")]
    [Min(0.2f)] public float maxCellSize = 1.32f;

    [Tooltip("Reference cell size where prefab scale = 1.0.")]
    [Min(0.2f)] public float referenceCellSize = 0.95f;

    [Header("Auto-Fit Behaviour")]
    [Tooltip("When enabled, scales and centers around the occupied vehicle bounding box rather than empty grid margins.")]
    public bool fitToOccupiedBounds = true;

    [Tooltip("Extra virtual border cells around occupied vehicles when fitToOccupiedBounds is enabled.")]
    [Min(0f)] public float occupiedBorderCells = 0.35f;

    [Tooltip("Inner padding inside PlayfieldBounds (world units) reserved before fitting grid & exit lanes.")]
    public Vector2 playfieldPadding = new Vector2(0.32f, 0.32f);

    [Tooltip("Exit road lane offset outside the grid expressed as a multiple of CurrentCellSize.")]
    [Range(0.2f, 1.5f)] public float roadCorridorRatio = 0.55f;

    [Header("Vehicle Scale Limits")]
    [Min(0.2f)] public float minVehicleScale = 0.60f;
    [Min(0.2f)] public float maxVehicleScale = 1.38f;
}
