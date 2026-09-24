using UnityEngine;

/// <summary>
/// A straight-flying shot, used for both player lasers and enemy projectiles.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : PooledObject
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 position, Vector2 direction, float speed)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));

        MarkActive();
        gameObject.SetActive(true);

        // The body is only simulated while active, so velocity is set after SetActive
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BoundaryZone _))
        {
            ReturnToPool();
        }
    }
}
