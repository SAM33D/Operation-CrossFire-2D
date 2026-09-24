using UnityEngine;

/// <summary>
/// Hull points, taking hits, and the invulnerability window after a hit.
/// </summary>
[RequireComponent(typeof(ShipGunner))]
public class ShipHealth : MonoBehaviour
{
    [SerializeField] private int maxHull = 3;
    [Tooltip("Seconds after a hit during which no more damage is taken.")]
    [SerializeField] private float invulnerabilityDuration = 1f;

    private ShipGunner gunner;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int hull;
    private float invulnerableTimer;

    public int Hull => hull;

    private void Awake()
    {
        gunner = GetComponent<ShipGunner>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Initialize()
    {
        hull = maxHull;
        invulnerableTimer = 0f;
    }

    public void Tick(float dt)
    {
        if (invulnerableTimer <= 0f) return;

        invulnerableTimer -= dt;
        if (invulnerableTimer <= 0f) spriteRenderer.color = originalColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.Instance.IsPlaying) return;

        // The collision matrix only lets enemies, debris and enemy projectiles reach the ship
        if (other.TryGetComponent(out PooledObject threat) && threat.IsActive)
        {
            threat.ReturnToPool();
            TryTakeDamage();
        }
    }

    private void TryTakeDamage()
    {
        if (gunner.IsShieldActive || invulnerableTimer > 0f) return;

        hull--;
        invulnerableTimer = invulnerabilityDuration;

        Color faded = originalColor;
        faded.a = 0.5f;
        spriteRenderer.color = faded;

        GameManager.Instance.OnHullChanged(hull);
    }
}
