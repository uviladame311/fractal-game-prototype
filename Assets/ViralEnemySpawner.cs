using UnityEngine;

public class ViralEnemySpawner : MonoBehaviour
{
    [Header("Viral Enemy Spawn Settings")]
    public GameObject viralEnemyPrefab;
    public float spawnRate = 0.2f;        // Viral enemies per second (much slower than nutrients)
    public float spawnRadius = 12f;       // How far from center to spawn
    public int maxViralEnemies = 10;      // Maximum viral enemies on screen
    public float minDistanceFromPlayer = 5f; // Don't spawn too close to player cells

    private float nextSpawnTime = 0f;
    private int currentViralEnemyCount = 0;

    void Update()
    {
        // Check if it's time to spawn and we haven't hit the max
        if (Time.time >= nextSpawnTime && currentViralEnemyCount < maxViralEnemies)
        {
            SpawnViralEnemy();
            nextSpawnTime = Time.time + (1f / spawnRate);
        }

        // Update count by finding all viral enemies
        UpdateViralEnemyCount();
    }

    void SpawnViralEnemy()
    {
        Vector2 spawnPosition;
        int attempts = 0;
        
        // Try to find a good spawn position away from player cells
        do
        {
            spawnPosition = Random.insideUnitCircle * spawnRadius;
            attempts++;
        } 
        while (IsTooCloseToPlayerCells(spawnPosition) && attempts < 10);

        // Create viral enemy prefab or basic cell with viral component
        GameObject newViralEnemy;
        
        if (viralEnemyPrefab != null)
        {
            Vector3 spawn3D = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
            newViralEnemy = Instantiate(viralEnemyPrefab, spawn3D, Quaternion.identity);
        }
        else
        {
            // Find a player cell to copy the structure from for proper 2D setup
            CellController playerCell = FindObjectOfType<CellController>();
            if (playerCell != null)
            {
                Vector3 spawn3D = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
                newViralEnemy = Instantiate(playerCell.gameObject, spawn3D, Quaternion.identity);
                
                // Remove player control components
                CellSelectable selectable = newViralEnemy.GetComponent<CellSelectable>();
                if (selectable != null)
                {
                    Destroy(selectable);
                }
                
                // Set as non-player controlled
                CellController viralCellController = newViralEnemy.GetComponent<CellController>();
                if (viralCellController != null)
                {
                    viralCellController.isPlayerControlled = false;
                    viralCellController.currentSize = 1f;
                }
                
                // Add viral behavior
                newViralEnemy.AddComponent<ViralEnemy>();
            }
            else
            {
                Debug.LogError("No player cell found to copy structure from!");
                return;
            }
        }

        // Force Z position to 0 for 2D
        Vector3 pos = newViralEnemy.transform.position;
        pos.z = 0f;
        newViralEnemy.transform.position = pos;

        currentViralEnemyCount++;
        Debug.Log("Spawned viral enemy at: " + newViralEnemy.transform.position);
    }

    bool IsTooCloseToPlayerCells(Vector2 position)
    {
        CellController[] playerCells = FindObjectsOfType<CellController>();
        
        foreach (CellController cell in playerCells)
        {
            // Skip viral enemies
            if (cell.GetComponent<ViralEnemy>() != null) continue;
            
            if (Vector2.Distance(position, cell.transform.position) < minDistanceFromPlayer)
            {
                return true;
            }
        }
        
        return false;
    }

    void UpdateViralEnemyCount()
    {
        ViralEnemy[] viralEnemies = FindObjectsOfType<ViralEnemy>();
        currentViralEnemyCount = viralEnemies.Length;
    }

    // Call this when a viral enemy is destroyed
    public void ViralEnemyDestroyed()
    {
        currentViralEnemyCount--;
    }
}