using UnityEngine;

public class ViralEnemy : MonoBehaviour
{
    [Header("Viral Enemy Settings")]
    public float moveSpeed = 3f;
    public float infectionRadius = 1.5f;
    public float infectionDamage = 0.5f;
    public float infectionCooldown = 2f;
    public float seekRadius = 8f;
    public float reproductionSize = 2f;
    public float reproductionCooldown = 10f;

    private Rigidbody2D rb;
    private float currentSize = 1f;
    private float lastInfectionTime = 0f;
    private float lastReproductionTime = 0f;
    private Vector3 targetPosition;
    private bool hasTarget = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        targetPosition = transform.position;
        
        // Make viral enemies visually distinct
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        // Add trigger collider for infection
        CircleCollider2D infectionCollider = gameObject.AddComponent<CircleCollider2D>();
        infectionCollider.isTrigger = true;
        infectionCollider.radius = infectionRadius;

        // Add rigidbody if not present
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
        }

        // Add tag for identification
        gameObject.tag = "ViralEnemy";
    }

    void Update()
    {
        if (!hasTarget || Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            FindNewTarget();
        }

        MoveTowardsTarget();
        CheckForReproduction();
    }

    void FindNewTarget()
    {
        // Look for nearby cells to infect
        GameObject nearestCell = FindNearestCell();
        
        if (nearestCell != null)
        {
            SetTarget(nearestCell.transform.position);
        }
        else
        {
            // Random movement if no cells nearby
            Vector3 randomDirection = Random.insideUnitCircle * 3f;
            SetTarget(transform.position + randomDirection);
        }
    }

    GameObject FindNearestCell()
    {
        CellController[] cells = FindObjectsOfType<CellController>();
        GameObject nearest = null;
        float shortestDistance = seekRadius;

        foreach (CellController cell in cells)
        {
            // Skip other viral enemies
            if (cell.GetComponent<ViralEnemy>() != null) continue;

            float distance = Vector3.Distance(transform.position, cell.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = cell.gameObject;
            }
        }

        return nearest;
    }

    void SetTarget(Vector3 newTarget)
    {
        targetPosition = newTarget;
        hasTarget = true;
    }

    void MoveTowardsTarget()
    {
        if (hasTarget && rb != null)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time - lastInfectionTime < infectionCooldown) return;

        CellController cell = other.GetComponent<CellController>();
        if (cell != null && cell.GetComponent<ViralEnemy>() == null)
        {
            InfectCell(cell);
            lastInfectionTime = Time.time;
        }
    }

    void InfectCell(CellController cell)
    {
        Debug.Log("Viral enemy infected: " + cell.name);
        
        // Reduce cell size
        cell.currentSize -= infectionDamage;
        
        if (cell.currentSize <= 0.5f)
        {
            // Convert cell to viral enemy
            ConvertToViralEnemy(cell.gameObject);
        }
        else
        {
            cell.UpdateCellSize();
        }

        // Grow the viral enemy
        currentSize += infectionDamage * 0.5f;
        UpdateSize();
    }

    void ConvertToViralEnemy(GameObject cellObject)
    {
        Debug.Log("Converting cell to viral enemy: " + cellObject.name);
        
        // Remove player control components
        CellSelectable selectable = cellObject.GetComponent<CellSelectable>();
        if (selectable != null)
        {
            // Remove from selection if selected
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.selectedUnits.Remove(selectable);
            }
            Destroy(selectable);
        }

        // Add viral enemy component
        ViralEnemy viralComponent = cellObject.AddComponent<ViralEnemy>();
        viralComponent.currentSize = 1f;
        viralComponent.UpdateSize();

        // Change visual appearance
        SpriteRenderer spriteRenderer = cellObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
    }

    void CheckForReproduction()
    {
        if (currentSize >= reproductionSize && Time.time - lastReproductionTime > reproductionCooldown)
        {
            ReproduceViralEnemy();
            lastReproductionTime = Time.time;
        }
    }

    void ReproduceViralEnemy()
    {
        Debug.Log("Viral enemy reproducing!");
        
        // Reset current size
        currentSize = 1f;
        
        // Create new viral enemy at Z=0
        Vector3 spawnPos = transform.position + Vector3.right * 1.5f;
        spawnPos.z = 0f;
        GameObject newViral = Instantiate(gameObject, spawnPos, Quaternion.identity);
        
        ViralEnemy newViralComponent = newViral.GetComponent<ViralEnemy>();
        newViralComponent.currentSize = 1f;
        newViralComponent.UpdateSize();
        
        UpdateSize();
    }

    void UpdateSize()
    {
        transform.localScale = Vector3.one * currentSize;
        
        // Keep at Z = 0
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;
    }
}