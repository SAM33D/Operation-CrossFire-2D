using UnityEngine;

public enum ControlType { Left, Right, Boost, Aim, Fire, Shield }

/// <summary>
/// An on-screen control area. Says which control it is and whether a screen point is inside it.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchControl : MonoBehaviour
{
    [SerializeField] private ControlType controlType;

    private RectTransform rectTransform;

    public ControlType Type => controlType;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    public bool Contains(Vector2 screenPoint)
    {
        // null camera because the Canvas is Screen Space - Overlay
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, null);
    }
}
