using UnityEngine;

public class CellAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 3f;
    public float nutrientSeekRadius = 5f;
    public float arrivalDistance = 0.5f;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Rigidbody2D rb;
    private CellController cellController;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cellController = GetComponent<CellController>();
        targetPosition = transform.position;
    }

    void Update()
    {
        if (enabled && cellController != null)
        {
            if (cellController.isPlayerControlled)
            {
                // Player controlled - only move if we have a target from a move command
                if (hasTarget)
                {
                    // Check if we've reached the target
                    if (Vector3.Distance(transform.position, targetPosition) < arrivalDistance)
                    {
                        hasTarget = false;
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector2.zero;
                        }
                    }
                    else
                    {
                        MoveTowardsTarget();
                    }
                }
            }
            else
            {
                // AI controlled - normal behavior
                if (!hasTarget || Vector3.Distance(transform.position, targetPosition) < arrivalDistance)
                {
                    FindNewTarget();
                }

                MoveTowardsTarget();
            }
        }
    }

    void FindNewTarget()
    {
        // Look for nearby nutrients
        GameObject nearestNutrient = FindNearestNutrient();

        if (nearestNutrient != null)
        {
            SetTarget(nearestNutrient.transform.position);
        }
        else
        {
            // Random wander - but not too far
            Vector3 randomDirection = Random.insideUnitCircle * 2f; // Smaller range
            Vector3 newTarget = transform.position + randomDirection;
            SetTarget(newTarget);
        }
    }

    GameObject FindNearestNutrient()
    {
        // Find all objects with Nutrient component instead of relying on tags
        Nutrient[] allNutrients = FindObjectsByType<Nutrient>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float shortestDistance = nutrientSeekRadius;

        foreach (Nutrient nutrient in allNutrients)
        {
            float distance = Vector3.Distance(transform.position, nutrient.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = nutrient.gameObject;
            }
        }

        return nearest;
    }

    void MoveTowardsTarget()
    {
        if (hasTarget && rb != null)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;

            // Use the same movement speed as the cell controller
            float currentMoveSpeed = moveSpeed;
            if (cellController != null)
            {
                currentMoveSpeed = cellController.moveSpeed; // Use the cell's actual speed
            }

            rb.linearVelocity = direction * currentMoveSpeed;
        }
    }

    public void SetTarget(Vector3 newTarget)
    {
        targetPosition = newTarget;
        hasTarget = true;
    }

    public void ClearTarget()
    {
        hasTarget = false;
    }

    public bool HasTarget()
    {
        return hasTarget;
    }
}