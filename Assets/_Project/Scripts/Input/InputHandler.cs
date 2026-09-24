using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns raw input into control values for the ship. Knows about controls, not players or roles.
/// </summary>
public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;

    [Header("Touch")]
    [Tooltip("Every TouchControl on both panels, both layouts.")]
    [SerializeField] private TouchControl[] touchControls;
    [Tooltip("Reticle movement per unit of finger drag. 1 = the reticle moves exactly as far as the finger.")]
    [SerializeField] private float aimDragSensitivity = 2f;

    private readonly Dictionary<int, ControlType> touchOwners = new Dictionary<int, ControlType>(10);
    private int[] holdCounts;
    private float unitsPerPixel;

    private bool useDevControls;
    private bool devInputLocked;
    private bool devLeftHeld;
    private bool devRightHeld;
    private bool devFireHeld;
    private Vector3 lastMousePosition;

    #region Public Output
    public float MoveDirection { get; private set; }
    public bool BoostPressed { get; private set; }
    public bool FireHeld { get; private set; }
    public bool ShieldPressed { get; private set; }
    public Vector2 AimDragDelta { get; private set; }
    public bool HasMouseAim { get; private set; }
    public Vector2 MouseAimPoint { get; private set; }
    #endregion

    private void Awake()
    {
        useDevControls = Application.isEditor || !Application.isMobilePlatform;
        holdCounts = new int[System.Enum.GetValues(typeof(ControlType)).Length];

        // Stops touches also arriving as mouse clicks on the device
        Input.simulateMouseWithTouches = false;
        unitsPerPixel = 2f * gameCamera.orthographicSize / Screen.height;
    }

    public void Tick()
    {
        ResetOutputs();

        ReadTouches();
        if (useDevControls) ReadDevControls();

        bool leftHeld = holdCounts[(int)ControlType.Left] > 0 || devLeftHeld;
        bool rightHeld = holdCounts[(int)ControlType.Right] > 0 || devRightHeld;
        MoveDirection = (rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f);

        FireHeld = holdCounts[(int)ControlType.Fire] > 0 || devFireHeld;
    }

    private void ResetOutputs()
    {
        MoveDirection = 0f;
        BoostPressed = false;
        FireHeld = false;
        ShieldPressed = false;
        AimDragDelta = Vector2.zero;
        HasMouseAim = false;

        devLeftHeld = false;
        devRightHeld = false;
        devFireHeld = false;
    }

    #region Touch Input
    private void ReadTouches()
    {
        // GetTouch instead of Input.touches, which allocates a new array every call
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchHeld(touch);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnded(touch);
                    break;
            }
        }
    }

    private void OnTouchBegan(Touch touch)
    {
        for (int i = 0; i < touchControls.Length; i++)
        {
            TouchControl control = touchControls[i];
            if (!control.gameObject.activeInHierarchy || !control.Contains(touch.position)) continue;

            ControlType type = control.Type;
            touchOwners[touch.fingerId] = type;
            holdCounts[(int)type]++;

            if (type == ControlType.Boost) BoostPressed = true;
            else if (type == ControlType.Shield) ShieldPressed = true;
            return;
        }
    }

    private void OnTouchHeld(Touch touch)
    {
        // Ownership never changes, so a finger that started on Left stays Left anywhere on screen
        if (!touchOwners.TryGetValue(touch.fingerId, out ControlType type)) return;

        if (type == ControlType.Aim)
        {
            AimDragDelta += touch.deltaPosition * unitsPerPixel * aimDragSensitivity;
        }
    }

    private void OnTouchEnded(Touch touch)
    {
        if (!touchOwners.TryGetValue(touch.fingerId, out ControlType type)) return;

        holdCounts[(int)type]--;
        touchOwners.Remove(touch.fingerId);
    }
    #endregion

    #region Editor Keyboard & Mouse
    private void ReadDevControls()
    {
        // Only when the mouse moves, so touch dragging (e.g. via Unity Remote) isn't overridden
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition != lastMousePosition)
        {
            lastMousePosition = mousePosition;
            HasMouseAim = true;
            MouseAimPoint = gameCamera.ScreenToWorldPoint(mousePosition);
        }

        // After a cancel, anything still held is ignored until everything is released
        if (devInputLocked)
        {
            if (AnyDevControlHeld()) return;
            devInputLocked = false;
        }

        devLeftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        devRightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        devFireHeld = Input.GetMouseButton(0);

        if (Input.GetKeyDown(KeyCode.Space)) BoostPressed = true;
        if (Input.GetMouseButtonDown(1)) ShieldPressed = true;
    }

    private bool AnyDevControlHeld()
    {
        return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.Space)
            || Input.GetMouseButton(0) || Input.GetMouseButton(1);
    }
    #endregion

    #region Cancel
    public void CancelAllInput()
    {
        // Fingers still down are no longer owned, so they're ignored until lifted and pressed again
        touchOwners.Clear();
        System.Array.Clear(holdCounts, 0, holdCounts.Length);

        ResetOutputs();
        devInputLocked = true;
    }
    #endregion
}
