using UnityEngine;

public class EnemyCell : MonoBehaviour
{
    [Header("Enemy Cell Settings")]
    public Color enemyColor = Color.blue;
    public float aggressionRadius = 6f;
    public float fleeRadius = 4f;
    public float reproductionSize = 2.5f;
    public float reproductionCooldown = 15f;

    private CellController cellController;
    private CellAI cellAI;
    private float lastReproductionTime = 0f;

    void Start()
    {
        // Get required components
        cellController = GetComponent<CellController>();
        cellAI = GetComponent<CellAI>();

        // Ensure this is not player controlled (should already be set in prefab)
        if (cellController != null)
        {
            cellController.isPlayerControlled = false;
        }

        // Set visual appearance (should already be blue in prefab, but ensure it matches settings)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = enemyColor;
        }

        // Modify AI behavior for enemy tactics
        if (cellAI != null)
        {
            cellAI.nutrientSeekRadius = aggressionRadius;
            cellAI.enabled = true; // Make sure AI is enabled
        }
    }

    void Update()
    {
        if (cellController != null && cellAI != null)
        {
            // Check for reproduction
            if (cellController.currentSize >= reproductionSize && 
                Time.time - lastReproductionTime > reproductionCooldown)
            {
                ReproduceEnemyCell();
                lastReproductionTime = Time.time;
            }
        }
    }


    void ReproduceEnemyCell()
    {
        // Create new enemy cell at Z=0
        Vector3 spawnPos = transform.position + Vector3.right * 1.5f;
        spawnPos.z = 0f;
        GameObject newEnemyCell = Instantiate(gameObject, spawnPos, Quaternion.identity);
        
        // Reset sizes
        cellController.currentSize = cellController.currentSize / 2f;
        
        CellController newCellController = newEnemyCell.GetComponent<CellController>();
        newCellController.currentSize = cellController.currentSize;
        newCellController.isPlayerControlled = false;
        
        // Update both cells
        cellController.UpdateCellSize();
        newCellController.UpdateCellSize();
        
        // Reset reproduction timer
        EnemyCell newEnemyComponent = newEnemyCell.GetComponent<EnemyCell>();
        newEnemyComponent.lastReproductionTime = Time.time;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle nutrient eating - cell eating is now handled by CellController
        Nutrient nutrient = other.GetComponent<Nutrient>();
        if (nutrient != null)
        {
            cellController.EatNutrient(nutrient.nutritionValue);
            Destroy(other.gameObject);
        }
    }
}