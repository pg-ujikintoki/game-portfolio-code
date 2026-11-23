using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private EnemyBase enemyPrefab;

    [Header("Respawn")]
    [SerializeField] private bool respawnable = true;
    [SerializeField] private bool oneTimeOnly = false;
    [SerializeField] private float respawnCooldown = 2f;

    [Header("Distance")]
    [SerializeField] private float spawnDistance = 12f;
    [SerializeField] private float releaseDistance = 18f;
    [SerializeField] private float despawnDistance = 20f;

    [Header("References")]
    [SerializeField] private Transform player;

    private EnemyBase currentEnemy;
    private bool hasBeenDefeated;
    private bool canRespawn;
    private bool spawningEnabled = true;
    private float defeatedTime;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (!spawningEnabled || player == null || enemyPrefab == null)
        {
            return;
        }

        float distance = Vector2.Distance(player.position, transform.position);

        if (currentEnemy != null)
        {
            if (distance >= despawnDistance)
            {
                DespawnCurrentEnemy();
                hasBeenDefeated = false;
                canRespawn = false;
            }

            return;
        }

        if (!hasBeenDefeated)
        {
            if (distance <= spawnDistance)
            {
                SpawnEnemy();
            }

            return;
        }

        if (!oneTimeOnly && respawnable && distance >= releaseDistance)
        {
            bool cooldownElapsed = Time.time >= defeatedTime + respawnCooldown;
            if (cooldownElapsed)
            {
                canRespawn = true;
            }
        }

        if (canRespawn && distance <= spawnDistance)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null || currentEnemy != null)
        {
            return;
        }

        currentEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        currentEnemy.InitializeFromSpawner(this);
        canRespawn = false;
    }

    public void StopSpawning()
    {
        spawningEnabled = false;
        DespawnCurrentEnemy();
        hasBeenDefeated = true;
        canRespawn = false;
    }

    public void DespawnCurrentEnemy()
    {
        if (currentEnemy == null)
        {
            return;
        }

        Destroy(currentEnemy.gameObject);
        currentEnemy = null;
    }

    public void NotifyEnemyDefeated(EnemyBase enemy)
    {
        if (enemy == null || enemy != currentEnemy)
        {
            return;
        }

        currentEnemy = null;
        hasBeenDefeated = true;
        defeatedTime = Time.time;
        canRespawn = false;
    }

    private void OnDrawGizmosSelected()
    {
        DrawRadius(spawnDistance, Color.green);
        DrawRadius(releaseDistance, Color.yellow);
        DrawRadius(despawnDistance, Color.red);
    }

    private void DrawRadius(float radius, Color color)
    {
        if (radius < 0f)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnValidate()
    {
        spawnDistance = Mathf.Max(0f, spawnDistance);
        releaseDistance = Mathf.Max(spawnDistance, releaseDistance);
        despawnDistance = Mathf.Max(releaseDistance, despawnDistance);
        respawnCooldown = Mathf.Max(0f, respawnCooldown);
    }
}
