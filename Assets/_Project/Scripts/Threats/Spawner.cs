using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns threats on a timer using the current phase values, and fires enemy projectiles.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("Pools")]
    [SerializeField] private ObjectPool enemyPool;
    [SerializeField] private ObjectPool debrisPool;
    [SerializeField] private ObjectPool breachPool;
    [SerializeField] private ObjectPool enemyProjectilePool;

    [Header("Spawning")]
    [Tooltip("Seconds between spawns before the phase multiplier.")]
    [SerializeField] private float baseSpawnInterval = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float debrisChance = 0.3f;
    [Tooltip("Only used in phases that allow breach hazards.")]
    [Range(0f, 1f)] [SerializeField] private float breachChance = 0.15f;
    [SerializeField] private float spawnEdgePadding = 0.5f;

    [Header("Enemy Projectiles")]
    [SerializeField] private float enemyProjectileBaseSpeed = 5f;

    private Playfield playfield;
    private PhaseData currentPhase;
    private float spawnTimer;
    private bool isSpawning;

    private float CurrentInterval => baseSpawnInterval * currentPhase.spawnIntervalMultiplier;

    public void Initialize()
    {
        playfield = GameManager.Instance.Playfield;
    }

    #region Phase
    public void ApplyPhase(PhaseData phase)
    {
        currentPhase = phase;
    }
    #endregion

    #region Spawning
    public void Begin()
    {
        isSpawning = true;
        spawnTimer = CurrentInterval;
    }

    public void Stop()
    {
        isSpawning = false;
    }

    public void Tick(float dt)
    {
        if (!isSpawning) return;

        spawnTimer -= dt;
        if (spawnTimer > 0f) return;

        SpawnHazard();
        spawnTimer += CurrentInterval;
    }

    private void SpawnHazard()
    {
        float x = Random.Range(playfield.MinX + spawnEdgePadding, playfield.MaxX - spawnEdgePadding);
        Vector2 position = new Vector2(x, playfield.SpawnY);

        PickPool().Get<Hazard>().Launch(position, currentPhase.enemySpeedMultiplier);
    }

    private ObjectPool PickPool()
    {
        float roll = Random.value;

        if (roll < debrisChance)
        {
            return debrisPool;
        }

        if (currentPhase.spawnBreachHazards && roll < debrisChance + breachChance)
        {
            return breachPool;
        } 
        
        return enemyPool;
    }
    #endregion

    #region Bomb
    public void ClearAllThreats()
    {
        KillAllHazards(enemyPool);
        KillAllHazards(debrisPool);
        KillAllHazards(breachPool);
        enemyProjectilePool.ReturnAllActive();
    }

    private static void KillAllHazards(ObjectPool pool)
    {
        IReadOnlyList<PooledObject> hazards = pool.AllObjects;
        for (int i = 0; i < hazards.Count; i++)
        {
            ((Hazard)hazards[i]).Kill();
        }
    }
    #endregion

    #region Enemy Projectiles
    public void FireEnemyProjectile(Vector2 position)
    {
        float speed = enemyProjectileBaseSpeed * currentPhase.enemyProjectileSpeedMultiplier;
        enemyProjectilePool.Get<Projectile>().Launch(position, Vector2.down, speed);
    }
    #endregion
}
