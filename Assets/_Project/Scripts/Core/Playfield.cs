using UnityEngine;

/// <summary>
/// Playfield bounds from the camera, and placement of the four boundary triggers.
/// </summary>
public class Playfield : MonoBehaviour
{
    private const float BoundaryThickness = 1f;
    private const float SpawnHeightAboveTop = 1f;

    [Header("References")]
    [SerializeField] private Camera gameCamera;

    [Header("Boundaries")]
    [SerializeField] private BoxCollider2D bottomEdge;
    [SerializeField] private BoxCollider2D topCleanup;
    [SerializeField] private BoxCollider2D leftCleanup;
    [SerializeField] private BoxCollider2D rightCleanup;

    [Tooltip("Distance outside the screen for the top, left and right cleanup triggers.")]
    [SerializeField] private float cleanupMargin = 2f;

    public float MinX { get; private set; }
    public float MaxX { get; private set; }
    public float MinY { get; private set; }
    public float MaxY { get; private set; }

    public float SpawnY => MaxY + SpawnHeightAboveTop;

    public void Initialize()
    {
        CalculateBounds();
        PlaceBoundaries();
    }

    public Vector2 ClampToPlayfield(Vector2 point)
    {
        return new Vector2(Mathf.Clamp(point.x, MinX, MaxX), Mathf.Clamp(point.y, MinY, MaxY));
    }

    private void CalculateBounds()
    {
        float halfHeight = gameCamera.orthographicSize;
        float halfWidth = halfHeight * gameCamera.aspect;
        Vector3 centre = gameCamera.transform.position;

        MinX = centre.x - halfWidth;
        MaxX = centre.x + halfWidth;
        MinY = centre.y - halfHeight;
        MaxY = centre.y + halfHeight;
    }

    private void PlaceBoundaries()
    {
        float centreX = (MinX + MaxX) * 0.5f;
        float centreY = (MinY + MaxY) * 0.5f;
        float halfThickness = BoundaryThickness * 0.5f;

        // Longer than the screen so the corners overlap
        float horizontalLength = (MaxX - MinX) + 2f * (cleanupMargin + BoundaryThickness);
        float verticalLength = (MaxY - MinY) + 2f * (cleanupMargin + BoundaryThickness);

        Place(bottomEdge,   new Vector2(centreX, MinY - halfThickness),                 new Vector2(horizontalLength, BoundaryThickness));
        Place(topCleanup,   new Vector2(centreX, MaxY + cleanupMargin + halfThickness), new Vector2(horizontalLength, BoundaryThickness));
        Place(leftCleanup,  new Vector2(MinX - cleanupMargin - halfThickness, centreY), new Vector2(BoundaryThickness, verticalLength));
        Place(rightCleanup, new Vector2(MaxX + cleanupMargin + halfThickness, centreY), new Vector2(BoundaryThickness, verticalLength));
    }

    private static void Place(BoxCollider2D boundary, Vector2 position, Vector2 size)
    {
        boundary.transform.position = position;
        boundary.offset = Vector2.zero;
        boundary.size = size;
    }
}
