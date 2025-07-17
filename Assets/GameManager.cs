using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    public GameObject playerCellPrefab;
    public float respawnDelay = 2f;
    public Vector3 respawnPosition = Vector3.zero;
    public float respawnRadius = 5f;

    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Fix all existing cells to have correct maxSize values
        FixAllExistingCells();
        
        // If no player cells exist at start, create one
        if (GetPlayerCellCount() == 0)
        {
            SpawnPlayerCell();
        }
    }

    private bool isRespawning = false;

    void Update()
    {
        // Check if all player cells are dead
        if (GetPlayerCellCount() == 0 && !isRespawning)
        {
            StartCoroutine(RespawnPlayerAfterDelay());
        }
        
        // Press F to fix all cells manually
        if (Input.GetKeyDown(KeyCode.F))
        {
            FixAllExistingCells();
        }
        
        // Press G to debug all cell sizes
        if (Input.GetKeyDown(KeyCode.G))
        {
            DebugAllCellSizes();
        }
        
    }

    public int GetPlayerCellCount()
    {
        CellController[] allCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
        int playerCellCount = 0;

        foreach (CellController cell in allCells)
        {
            if (cell.isPlayerControlled)
            {
                playerCellCount++;
            }
        }

        return playerCellCount;
    }

    IEnumerator RespawnPlayerAfterDelay()
    {
        isRespawning = true;
        Debug.Log("All player cells destroyed! Respawning in " + respawnDelay + " seconds...");
        
        yield return new WaitForSeconds(respawnDelay);
        
        // Double-check that player is still dead (in case they divided at the last moment)
        if (GetPlayerCellCount() == 0)
        {
            SpawnPlayerCell();
        }
        
        isRespawning = false;
    }

    void SpawnPlayerCell()
    {
        Vector3 spawnPos = respawnPosition;
        
        // Add some randomness to avoid spawning on enemies
        Vector2 randomOffset = Random.insideUnitCircle * respawnRadius;
        spawnPos += new Vector3(randomOffset.x, randomOffset.y, 0);
        spawnPos.z = 0; // Ensure 2D position

        GameObject newPlayerCell;
        
        if (playerCellPrefab != null)
        {
            // Use the assigned prefab - this is the correct way
            newPlayerCell = Instantiate(playerCellPrefab, spawnPos, Quaternion.identity);
            Debug.Log("***** PLAYER RESPAWNED FROM PREFAB - Position: " + spawnPos + " *****");
        }
        else
        {
            // No prefab assigned - use the EnemyCell prefab as template and modify it
            GameObject enemyCellPrefab = null;
            EnemyCellSpawner spawner = FindFirstObjectByType<EnemyCellSpawner>();
            if (spawner != null)
            {
                enemyCellPrefab = spawner.enemyCellPrefab;
            }
            
            if (enemyCellPrefab != null)
            {
                // Use enemy cell prefab as template
                newPlayerCell = Instantiate(enemyCellPrefab, spawnPos, Quaternion.identity);
                
                // Convert it to a player cell
                CellController cellController = newPlayerCell.GetComponent<CellController>();
                if (cellController != null)
                {
                    cellController.isPlayerControlled = true;
                }
                
                // Change color to white for player
                SpriteRenderer sr = newPlayerCell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                }
                
                // Remove enemy-specific component
                EnemyCell enemyComponent = newPlayerCell.GetComponent<EnemyCell>();
                if (enemyComponent != null)
                {
                    Destroy(enemyComponent);
                }
                
                Debug.Log("***** PLAYER RESPAWNED FROM ENEMY TEMPLATE - Position: " + spawnPos + " *****");
            }
            else
            {
                Debug.LogError("No player cell prefab and no enemy cell prefab found!");
                return;
            }
        }
        
        // Reset the cell to fresh state
        CellController controller = newPlayerCell.GetComponent<CellController>();
        if (controller != null)
        {
            controller.currentSize = 1f;
            controller.UpdateCellSize();
        }
        
        // Optional: Add visual feedback for respawn
        CreateRespawnEffect(spawnPos);
    }

    // Helper method to create a circle sprite if no prefab is available
    Sprite CreateCircleSprite()
    {
        // This creates a simple circle sprite programmatically
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                Vector2 center = new Vector2(32, 32);
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= 30)
                {
                    pixels[y * 64 + x] = Color.white;
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    void CreateRespawnEffect(Vector3 position)
    {
        // Create a simple visual effect to show where the player respawned
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "RespawnEffect";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 2f;
        
        // Make it glow/stand out
        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
        }
        
        // Remove collider so it doesn't interfere
        Destroy(effect.GetComponent<Collider>());
        
        // Auto-destroy after 1 second
        Destroy(effect, 1f);
    }

    void FixAllExistingCells()
    {
        // Find all existing cells and fix their stats
        CellController[] allCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
        
        foreach (CellController cell in allCells)
        {
            // Fix maxSize for all cells
            cell.maxSize = 15f;
            cell.growthRate = 0.4f;
            
            // If the cell is already bigger than the old cap, don't shrink it
            if (cell.currentSize > 15f)
            {
                cell.currentSize = 15f;
            }
            
            // FORCE rebuild colliders by destroying and recreating them
            CircleCollider2D[] oldColliders = cell.GetComponents<CircleCollider2D>();
            foreach (CircleCollider2D col in oldColliders)
            {
                DestroyImmediate(col);
            }
            
            // Recreate colliders with correct sizes
            CircleCollider2D clickCollider = cell.gameObject.AddComponent<CircleCollider2D>();
            clickCollider.isTrigger = false;
            clickCollider.radius = 0.5f;
            
            CircleCollider2D triggerCollider = cell.gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.55f;
            
            cell.UpdateCellSize();
            
            // Make sure the cell has size text (for existing cells that might not have it)
            if (cell.transform.Find("SizeText") == null)
            {
                cell.CreateSizeText();
            }
            
            Debug.Log("Fixed cell: " + cell.gameObject.name + " - maxSize: " + cell.maxSize + ", currentSize: " + cell.currentSize);
        }
        
        Debug.Log("Fixed " + allCells.Length + " existing cells to have maxSize = 15f");
    }

    void DebugAllCellSizes()
    {
        CellController[] allCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
        
        Debug.Log("=== DEBUGGING ALL CELL SIZES ===");
        foreach (CellController cell in allCells)
        {
            Vector3 scale = cell.transform.localScale;
            CircleCollider2D[] colliders = cell.GetComponents<CircleCollider2D>();
            
            Debug.Log($"Cell: {cell.gameObject.name}");
            Debug.Log($"  currentSize: {cell.currentSize}");
            Debug.Log($"  transform.localScale: {scale}");
            Debug.Log($"  isPlayerControlled: {cell.isPlayerControlled}");
            
            foreach (CircleCollider2D col in colliders)
            {
                float worldRadius = col.radius * scale.x; // Actual world radius
                Debug.Log($"  Collider radius: {col.radius}, world radius: {worldRadius}, isTrigger: {col.isTrigger}");
            }
            Debug.Log("---");
        }
        Debug.Log("=== END DEBUG ===");
    }

    public void NotifyPlayerCellDestroyed()
    {
        // This can be called when a player cell is destroyed for immediate checking
        // The Update loop will handle it anyway, but this allows for faster response
    }
}