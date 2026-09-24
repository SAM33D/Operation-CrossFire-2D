using UnityEngine;

/// <summary>
/// Pilot controls: horizontal movement, screen clamping and Boost.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShipPilot : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float heightAboveBottom = 1.5f;

    [Header("Boost")]
    [SerializeField] private float boostSpeedMultiplier = 1.75f;
    [SerializeField] private TimedAbility boost = new TimedAbility(1f, 4f);

    private Rigidbody2D rb;
    private Collider2D shipCollider;
    private InputHandler input;
    private Playfield playfield;

    private float halfWidth;
    private float shipY;
    private float targetX;

    public TimedAbility Boost => boost;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shipCollider = GetComponent<Collider2D>();
    }

    public void Initialize()
    {
        input = GameManager.Instance.Input;
        playfield = GameManager.Instance.Playfield;

        halfWidth = shipCollider.bounds.extents.x;
        shipY = playfield.MinY + heightAboveBottom;
        targetX = (playfield.MinX + playfield.MaxX) * 0.5f;

        transform.position = new Vector2(targetX, shipY);
    }

    public void Tick(float dt)
    {
        boost.Tick(dt);
        if (input.BoostPressed) boost.TryActivate();

        float speed = moveSpeed * (boost.IsActive ? boostSpeedMultiplier : 1f);

        targetX = Mathf.Clamp(targetX + input.MoveDirection * speed * dt,
                              playfield.MinX + halfWidth,
                              playfield.MaxX - halfWidth);

        rb.MovePosition(new Vector2(targetX, shipY));
    }

    public void CancelActions()
    {
        boost.Cancel();
    }
}
