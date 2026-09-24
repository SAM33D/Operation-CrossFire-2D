using UnityEngine;

/// <summary>
/// Marks a boundary trigger. The bottom edge is also the breach line.
/// </summary>
public class BoundaryZone : MonoBehaviour
{
    [SerializeField] private bool isBottomEdge;

    public bool IsBottomEdge => isBottomEdge;
}
