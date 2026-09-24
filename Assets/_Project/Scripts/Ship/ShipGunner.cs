using UnityEngine;

/// <summary>
/// Gunner controls: reticle aiming, firing at a fixed cadence, and Shield.
/// </summary>
public class ShipGunner : MonoBehaviour
{
    private const float ReticleStartHeight = 3f;
    private const float MinReticleHeightAboveMuzzle = 0.5f;

    [Header("References")]
    [Tooltip("Keep this outside the ship so it holds its aim while the ship moves.")]
    [SerializeField] private Transform reticle;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ObjectPool laserPool;

    [Header("Weapon")]
    [SerializeField] private float laserSpeed = 14f;
    [Tooltip("Seconds between shots while Fire is held.")]
    [SerializeField] private float fireCooldown = 0.25f;

    [Header("Shield")]
    [SerializeField] private TimedAbility shield = new TimedAbility(1.5f, 5f);
    [SerializeField] private GameObject shieldVisual;

    private InputHandler input;
    private Playfield playfield;
    private float fireTimer;

    public bool IsShieldActive => shield.IsActive;
    public TimedAbility Shield => shield;

    public void Initialize()
    {
        input = GameManager.Instance.Input;
        playfield = GameManager.Instance.Playfield;

        reticle.position = (Vector2)muzzle.position + Vector2.up * ReticleStartHeight;
        shieldVisual.SetActive(false);
        fireTimer = 0f;
    }

    public void Tick(float dt)
    {
        UpdateShield(dt);
        UpdateAim();
        UpdateFiring(dt);
    }

    public void CancelActions()
    {
        shield.Cancel();
        shieldVisual.SetActive(false);
    }

    #region Aiming

    private void UpdateAim()
    {
        Vector2 target = reticle.position;

        if (input.HasMouseAim) target = input.MouseAimPoint;
        target += input.AimDragDelta;

        target = playfield.ClampToPlayfield(target);

        float minY = muzzle.position.y + MinReticleHeightAboveMuzzle;
        if (target.y < minY) target.y = minY;

        reticle.position = target;
    }

    #endregion

    #region Firing

    private void UpdateFiring(float dt)
    {
        // Runs regardless of input, so rapid clicking can't fire faster
        if (fireTimer > 0f) fireTimer -= dt;

        if (!input.FireHeld || fireTimer > 0f) return;

        FireLaser();
        fireTimer += fireCooldown;
    }

    private void FireLaser()
    {
        Vector2 origin = muzzle.position;
        Vector2 toReticle = (Vector2)reticle.position - origin;
        Vector2 direction = toReticle.sqrMagnitude > 0.0001f ? toReticle.normalized : Vector2.up;

        laserPool.Get<Projectile>().Launch(origin, direction, laserSpeed);
    }

    #endregion

    #region Shield

    private void UpdateShield(float dt)
    {
        shield.Tick(dt);
        if (input.ShieldPressed) shield.TryActivate();

        if (shieldVisual.activeSelf != shield.IsActive) shieldVisual.SetActive(shield.IsActive);
    }

    #endregion
}
