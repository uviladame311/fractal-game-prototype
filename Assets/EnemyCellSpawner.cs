using UnityEngine;

public class EnemyCellSpawner : MonoBehaviour
{
    [Header("Enemy Cell Spawn Settings")]
    public GameObject enemyCellPrefab;
    public float spawnRate = 0.1f;        // Enemy cells per second (very slow)
    public float spawnRadius = 15f;       // How far from center to spawn
    public int maxEnemyCells = 5;         // Maximum enemy cells on screen
    public float minDistanceFromPlayer = 8f; // Don't spawn too close to player cells

    private float nextSpawnTime = 0f;
    private int currentEnemyCellCount = 0;

    void Update()
    {
        // Check if it's time to spawn and we haven't hit the max
        if (Time.time >= nextSpawnTime && currentEnemyCellCount < maxEnemyCells)
        {
            SpawnEnemyCell();
            nextSpawnTime = Time.time + (1f / spawnRate);
        }

        // Update count by finding all enemy cells
        UpdateEnemyCellCount();
    }

    void SpawnEnemyCell()
    {
        if (enemyCellPrefab == null)
        {
            Debug.LogError("EnemyCellPrefab is not assigned!");
            return;
        }

        Vector2 spawnPosition;
        int attempts = 0;
        
        // Try to find a good spawn position away from player cells
        do
        {
            spawnPosition = Random.insideUnitCircle * spawnRadius;
            attempts++;
        } 
        while (IsTooCloseToPlayerCells(spawnPosition) && attempts < 10);

        // Create enemy cell from prefab
        Vector3 spawn3D = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        GameObject newEnemyCell = Instantiate(enemyCellPrefab, spawn3D, Quaternion.identity);
        
        // The prefab should already have all components configured properly
        // Just ensure Z position is exactly 0 for 2D
        Vector3 pos = newEnemyCell.transform.position;
        pos.z = 0f;
        newEnemyCell.transform.position = pos;
        
        // Ensure enemy has same max size as player cells
        CellController enemyController = newEnemyCell.GetComponent<CellController>();
        if (enemyController != null)
        {
            enemyController.maxSize = 15f; // Same as player cells
            enemyController.growthRate = 0.4f; // Same as player cells
        }
        
        // Ensure the enemy has a trigger collider for eating interactions
        CircleCollider2D[] colliders = newEnemyCell.GetComponents<CircleCollider2D>();
        bool hasTriggerCollider = false;
        foreach (CircleCollider2D col in colliders)
        {
            if (col.isTrigger)
            {
                hasTriggerCollider = true;
                break;
            }
        }
        
        if (!hasTriggerCollider)
        {
            CircleCollider2D triggerCollider = newEnemyCell.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.6f;
        }
        
        currentEnemyCellCount++;
    }

    bool IsTooCloseToPlayerCells(Vector2 position)
    {
        CellController[] playerCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
        
        foreach (CellController cell in playerCells)
        {
            // Skip enemy cells
            if (cell.GetComponent<EnemyCell>() != null) continue;
            
            if (Vector2.Distance(position, cell.transform.position) < minDistanceFromPlayer)
            {
                return true;
            }
        }
        
        return false;
    }

    void UpdateEnemyCellCount()
    {
        EnemyCell[] enemyCells = FindObjectsByType<EnemyCell>(FindObjectsSortMode.None);
        currentEnemyCellCount = enemyCells.Length;
    }

    // Call this when an enemy cell is destroyed
    public void EnemyCellDestroyed()
    {
        currentEnemyCellCount--;
    }
}