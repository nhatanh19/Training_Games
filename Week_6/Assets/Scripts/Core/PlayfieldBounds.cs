using UnityEngine;

/// <summary>
/// Editor-editable world bounds defining where the dynamic vehicle grid and exit lanes fit.
/// </summary>
public class PlayfieldBounds : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(7.40f, 7.10f);
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.95f, 0.45f, 0.95f);

    public Vector2 Size => new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    public Vector3 Center => transform.position + new Vector3(offset.x, offset.y, 0f);

    public Rect WorldRect
    {
        get
        {
            Vector3 c = Center;
            Vector2 s = Size;
            return new Rect(c.x - s.x * 0.5f, c.y - s.y * 0.5f, s.x, s.y);
        }
    }

    private void OnDrawGizmos()
    {
        Rect rect = WorldRect;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(rect.center, new Vector3(rect.width, rect.height, 0.05f));

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        Gizmos.DrawWireCube(rect.center, new Vector3(rect.width * 0.92f, rect.height * 0.92f, 0.05f));
    }
}
