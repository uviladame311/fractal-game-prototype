using UnityEngine;

public class CellController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool isPlayerControlled = true; // New: determines if player can control this cell

    [Header("Trail Settings")]
    public float trailTime = 0.5f;

    [Header("Growth Settings")]
    public float currentSize = 1f;
    public float growthRate = 0.4f;
    public float maxSize = 15f;
    public float minSpeed = 2f;

    private TrailRenderer trailRenderer;
    private Rigidbody2D rb;
    private float baseSpeed;
    private TextMesh sizeText;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        trailRenderer = GetComponent<TrailRenderer>();
        baseSpeed = moveSpeed;

        if (trailRenderer != null)
        {
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0.05f;
        }

        UpdateCellSize();

        // Create size text display (only once)
        if (transform.Find("SizeText") == null)
        {
            CreateSizeText();
        }

        // Add required components for RTS system (only for player controlled cells)
        if (isPlayerControlled && GetComponent<CellSelectable>() == null)
            gameObject.AddComponent<CellSelectable>();

        if (GetComponent<CellAI>() == null)
            gameObject.AddComponent<CellAI>();

        // Ensure we have both types of colliders
        CircleCollider2D[] existingColliders = GetComponents<CircleCollider2D>();
        bool hasClickCollider = false;
        bool hasTriggerCollider = false;
        
        foreach (CircleCollider2D col in existingColliders)
        {
            if (col.isTrigger) hasTriggerCollider = true;
            else hasClickCollider = true;
        }
        
        if (!hasClickCollider)
        {
            CircleCollider2D clickCollider = gameObject.AddComponent<CircleCollider2D>();
            clickCollider.isTrigger = false; // For selection clicks
        }
        
        if (!hasTriggerCollider)
        {
            CircleCollider2D triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true; // For eating interactions
            triggerCollider.radius = 0.6f;
        }
    }

    void Update()
    {
        // Only respond to direct input if this cell is player controlled AND it's the only selected unit
        if (isPlayerControlled)
        {
            // Check if this is part of a group
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            bool isInGroup = selectionManager != null && selectionManager.selectedUnits.Count > 1;

            if (!isInGroup)
            {
                // Solo unit control - use WASD
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                Vector2 movement = new Vector2(horizontal, vertical);
                rb.linearVelocity = movement * moveSpeed;
            }
            // If in group, let GroupMovement handle WASD
        }

        // Division check stays the same
        if (currentSize >= maxSize && Input.GetKeyDown(KeyCode.Space))
        {
            DivideCell();
        }
    }

    public void EatNutrient(float nutritionValue)
    {
        Debug.Log("Ate nutrient worth: " + nutritionValue);

        currentSize += growthRate * nutritionValue;

        // Handle overflow by auto-dividing
        if (currentSize > maxSize)
        {
            float overflow = currentSize - maxSize;
            Debug.Log(gameObject.name + " exceeded maxSize! Overflow: " + overflow + " - Auto-dividing!");
            
            currentSize = maxSize;
            DivideCell();
            
            // Give the overflow to the new cell
            CellController[] myCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
            foreach (CellController cell in myCells)
            {
                if (cell != this && cell.isPlayerControlled == this.isPlayerControlled && 
                    Vector2.Distance(cell.transform.position, this.transform.position) < 2f)
                {
                    cell.currentSize = Mathf.Min(1f + overflow, cell.maxSize);
                    cell.UpdateCellSize();
                    break;
                }
            }
        }

        UpdateCellSize();
    }

    public void EatCell(CellController otherCell)
    {
        if (otherCell == null) return;

        // Gain size from eating the other cell
        float sizeGain = otherCell.currentSize * 0.8f; // Get 80% of the eaten cell's size
        
        currentSize += sizeGain;

        // Handle overflow by auto-dividing
        if (currentSize > maxSize)
        {
            float overflow = currentSize - maxSize;
            Debug.Log(gameObject.name + " exceeded maxSize after eating! Overflow: " + overflow + " - Auto-dividing!");
            
            currentSize = maxSize;
            DivideCell();
            
            // Give the overflow to the new cell
            CellController[] myCells = FindObjectsByType<CellController>(FindObjectsSortMode.None);
            foreach (CellController cell in myCells)
            {
                if (cell != this && cell.isPlayerControlled == this.isPlayerControlled && 
                    Vector2.Distance(cell.transform.position, this.transform.position) < 2f)
                {
                    cell.currentSize = Mathf.Min(1f + overflow, cell.maxSize);
                    cell.UpdateCellSize();
                    break;
                }
            }
        }

        UpdateCellSize();
        
        Debug.Log(gameObject.name + " ate " + otherCell.gameObject.name + " and grew to size " + currentSize);

        // Notify GameManager if a player cell is being eaten
        if (otherCell.isPlayerControlled && GameManager.Instance != null)
        {
            GameManager.Instance.NotifyPlayerCellDestroyed();
        }

        // Destroy the eaten cell
        Destroy(otherCell.gameObject);
    }

    public bool CanEat(CellController otherCell)
    {
        if (otherCell == null) return false;
        
        // Can eat if this cell is significantly larger (at least 30% bigger)
        return currentSize > otherCell.currentSize * 1.3f;
    }

    void DivideCell()
    {
        // Reset current cell to half size
        currentSize = currentSize / 2f;

        // Create new cell nearby
        GameObject newCell = Instantiate(gameObject, transform.position + Vector3.right * 1f, Quaternion.identity);
        // Force the new cell to Z = 0 for 2D physics
        newCell.transform.position = new Vector3(newCell.transform.position.x, newCell.transform.position.y, 0);

        CellController newCellController = newCell.GetComponent<CellController>();
        newCellController.currentSize = currentSize;
        newCellController.isPlayerControlled = this.isPlayerControlled; // Preserve parent's control status

        // CRITICAL: Fix the speed values
        newCellController.moveSpeed = this.moveSpeed;  // Copy current speed
        newCellController.baseSpeed = this.baseSpeed;  // Copy base speed

        // Update both cells properly
        newCellController.UpdateCellSize();
        UpdateCellSize();

        Debug.Log("Cell divided! New cell speed: " + newCellController.moveSpeed);

        // CRITICAL: Ensure the new cell has a non-trigger collider for mouse clicks
        CircleCollider2D[] existingColliders = newCell.GetComponents<CircleCollider2D>();
        bool hasClickCollider = false;

        foreach (CircleCollider2D col in existingColliders)
        {
            if (!col.isTrigger)
            {
                hasClickCollider = true;
                Debug.Log("New cell already has click collider");
                break;
            }
        }

        if (!hasClickCollider)
        {
            CircleCollider2D clickCollider = newCell.AddComponent<CircleCollider2D>();
            clickCollider.isTrigger = false;
            Debug.Log("Added click collider to new cell");
        }

        // Update both cells
        newCellController.UpdateCellSize();
        UpdateCellSize();

        Debug.Log("Cell divided! New cell should be clickable now.");
    }

    public void UpdateCellSize()
    {
        // UNIFIED SIZE SYSTEM - everything calculated from currentSize
        
        // 1. Visual size - sprite scale matches currentSize exactly
        transform.localScale = Vector3.one * currentSize;

        // 2. Keep cells at Z = 0 for 2D physics
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        // 3. Speed calculation
        float speedMultiplier = Mathf.Lerp(1f, minSpeed / baseSpeed, (currentSize - 1f) / (maxSize - 1f));
        moveSpeed = baseSpeed * speedMultiplier;

        // Debug NaN issues
        if (float.IsNaN(moveSpeed) || float.IsNaN(baseSpeed))
        {
            Debug.LogError("NaN speed detected! baseSpeed=" + baseSpeed + ", moveSpeed=" + moveSpeed);
            moveSpeed = 5f; // Fallback speed
            baseSpeed = 5f;
        }

        // 4. Colliders - calculated precisely from visual size
        UpdateColliderSizes();

        // 5. Trail effects
        if (trailRenderer != null)
        {
            trailRenderer.startWidth = 0.1f * currentSize;
            trailRenderer.endWidth = 0.05f * currentSize;
        }

        // 6. Size text
        UpdateSizeText();
        
    }

    // Helper functions to get exact radii
    public float GetVisualRadius()
    {
        // The actual visual radius of the cell
        return currentSize * 0.5f; // Unity sprite default radius is 0.5 at scale 1
    }
    
    public float GetEatingRadius()
    {
        // Eating radius should be slightly larger than visual
        return GetVisualRadius() * 1.1f; // 10% larger than visual
    }
    
    public float GetClickRadius()
    {
        // Click radius should match visual exactly
        return GetVisualRadius();
    }

    void UpdateColliderSizes()
    {
        CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
        foreach (CircleCollider2D collider in colliders)
        {
            if (collider.isTrigger)
            {
                // Eating collider - compensate for transform scale
                collider.radius = 0.55f / currentSize; // Divide by scale so final radius is 0.55
            }
            else
            {
                // Click collider - compensate for transform scale  
                collider.radius = 0.5f / currentSize; // Divide by scale so final radius is 0.5
            }
        }
    }

    public void CreateSizeText()
    {
        // Destroy existing text first
        Transform existingText = transform.Find("SizeText");
        if (existingText != null)
        {
            DestroyImmediate(existingText.gameObject);
        }
        
        // Create a child GameObject for the text
        GameObject textObject = new GameObject("SizeText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localScale = Vector3.one;

        // Add TextMesh component
        sizeText = textObject.AddComponent<TextMesh>();
        sizeText.text = currentSize.ToString("F1");
        sizeText.fontSize = 50; // Reasonable resolution
        sizeText.characterSize = 0.1f; // Readable size
        sizeText.anchor = TextAnchor.MiddleCenter;
        sizeText.alignment = TextAlignment.Center;
        
        // Set color based on cell type with good contrast
        if (isPlayerControlled)
        {
            sizeText.color = Color.black; // Black text on white player cells
        }
        else if (GetComponent<EnemyCell>() != null)
        {
            sizeText.color = Color.white; // White text on blue enemy cells
        }
        else
        {
            sizeText.color = Color.black; // Black text on other cells
        }

        // Make sure text renders in front
        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 10; // Render on top
        }
    }

    void UpdateSizeText()
    {
        // Always find the text object fresh to ensure we have the right reference
        Transform textTransform = transform.Find("SizeText");
        if (textTransform != null)
        {
            sizeText = textTransform.GetComponent<TextMesh>();
        }
        
        // If no text exists, create it
        if (sizeText == null)
        {
            CreateSizeText();
        }
        
        if (sizeText != null)
        {
            // Force update the text to current size
            sizeText.text = currentSize.ToString("F1");
            
            // Text should be readable but not huge
            // Fixed scale that's always visible
            float textScale = 0.3f; // Smaller readable size
            sizeText.transform.localScale = Vector3.one * textScale;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle cell eating - check distance explicitly
        CellController otherCell = other.GetComponent<CellController>();
        if (otherCell != null && otherCell != this)
        {
            // Calculate actual distance between cell centers
            float distance = Vector2.Distance(transform.position, otherCell.transform.position);
            
            // Calculate eating range: my visual radius + small buffer
            float myVisualRadius = currentSize * 0.5f;
            float eatingRange = myVisualRadius + 0.1f;
            
            // Only eat if within explicit distance AND size allows it
            if (distance <= eatingRange && CanEat(otherCell))
            {
                EatCell(otherCell);
            }
        }
    }
}