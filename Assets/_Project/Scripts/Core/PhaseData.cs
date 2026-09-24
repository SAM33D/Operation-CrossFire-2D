using UnityEngine;

/// <summary>
/// One row of the difficulty phase table.
/// </summary>
[System.Serializable]
public class PhaseData
{
    public string phaseName;

    [Tooltip("Round time in seconds when this phase begins.")]
    public float startTime;

    [Tooltip("Applies to enemies, debris and breach hazards.")]
    public float enemySpeedMultiplier = 1f;

    [Tooltip("Lower = more frequent spawns.")]
    public float spawnIntervalMultiplier = 1f;

    public float enemyProjectileSpeedMultiplier = 1f;

    public bool spawnBreachHazards;

    public PhaseData(string phaseName, float startTime, float enemySpeedMultiplier,
                     float spawnIntervalMultiplier, float enemyProjectileSpeedMultiplier, bool spawnBreachHazards)
    {
        this.phaseName = phaseName;
        this.startTime = startTime;
        this.enemySpeedMultiplier = enemySpeedMultiplier;
        this.spawnIntervalMultiplier = spawnIntervalMultiplier;
        this.enemyProjectileSpeedMultiplier = enemyProjectileSpeedMultiplier;
        this.spawnBreachHazards = spawnBreachHazards;
    }
}
