using UnityEngine;

/// <summary>
/// Duration + cooldown timer shared by Boost and Shield.
/// </summary>
[System.Serializable]
public class TimedAbility
{
    [SerializeField] private float duration;
    [Tooltip("Starts when the effect ends.")]
    [SerializeField] private float cooldown;

    private float activeTimer;
    private float cooldownTimer;

    public TimedAbility(float duration, float cooldown)
    {
        this.duration = duration;
        this.cooldown = cooldown;
    }

    public bool IsActive => activeTimer > 0f;
    public bool IsReady => !IsActive && cooldownTimer <= 0f;

    public float ActiveRemaining01 => duration > 0f ? Mathf.Clamp01(activeTimer / duration) : 0f;
    public float CooldownRemaining01 => IsActive || cooldown <= 0f ? 0f : Mathf.Clamp01(cooldownTimer / cooldown);

    public bool TryActivate()
    {
        if (!IsReady) return false;

        activeTimer = duration;
        return true;
    }

    public void Tick(float dt)
    {
        if (activeTimer > 0f)
        {
            activeTimer -= dt;
            if (activeTimer <= 0f)
            {
                activeTimer = 0f;
                cooldownTimer = cooldown;
            }
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer -= dt;
        }
    }

    public void Cancel()
    {
        if (!IsActive) return;

        activeTimer = 0f;
        cooldownTimer = cooldown;
    }
}
