using UnityEngine;

/// <summary>
/// A descending threat: enemy, debris or breach hazard, configured per prefab.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Hazard : PooledObject
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float baseSpeed = 2.5f;

    [Header("Behaviour")]
    [Tooltip("Loses the round if it reaches the bottom edge.")]
    [SerializeField] private bool isBreachHazard;
    [SerializeField] private bool canShoot;
    [SerializeField] private float fireInterval = 1.8f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Quaternion originalRotation;
    private int health;
    private float fireTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        // The prefab's own rotation, so the breach diamond stays a diamond
        originalRotation = transform.rotation;
    }

    public void Launch(Vector2 position, float speedMultiplier)
    {
        transform.SetPositionAndRotation(position, originalRotation);
        health = maxHealth;
        fireTimer = Random.Range(0.5f, fireInterval);
        spriteRenderer.color = originalColor;

        MarkActive();
        gameObject.SetActive(true);

        rb.linearVelocity = Vector2.down * baseSpeed * speedMultiplier;
        rb.angularVelocity = 0f;
    }

    private void Update()
    {
        if (!canShoot || !GameManager.Instance.IsPlaying) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        fireTimer += fireInterval;
        GameManager.Instance.Spawner.FireEnemyProjectile(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActive) return;

        if (other.TryGetComponent(out Projectile laser))
        {
            // Stops one laser damaging two hazards in the same frame
            if (!laser.IsActive) return;

            laser.ReturnToPool();
            TakeDamage(1);
        }
        else if (other.TryGetComponent(out BoundaryZone zone))
        {
            if (isBreachHazard && zone.IsBottomEdge) GameManager.Instance.OnBreachReachedBottom();
            ReturnToPool();
        }
    }

    private void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            GameManager.Instance.AddScore(scoreValue);
            ReturnToPool();
            return;
        }

        spriteRenderer.color = Color.Lerp(Color.black, originalColor, 0.4f + 0.6f * health / maxHealth);
    }
}
