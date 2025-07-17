using UnityEngine;
using System.Collections.Generic;

public class GroupMovement : MonoBehaviour
{
    [Header("Group Movement Settings")]
    public float groupMoveSpeed = 4f;
    public float cohesionStrength = 1f;
    public float cohesionRadius = 3f;

    private SelectionManager selectionManager;

    void Start()
    {
        selectionManager = FindObjectOfType<SelectionManager>();
    }

    void Update()
    {
        if (selectionManager != null && selectionManager.selectedUnits.Count > 1)
        {
            HandleGroupMovement();
        }
    }

    void LateUpdate()
    {
        if (selectionManager != null && selectionManager.selectedUnits.Count > 1)
        {
            ApplyCohesion();
        }
    }

    void HandleGroupMovement()
    {
        // Check for WASD input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Only override right-click movement if WASD is actively being pressed
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            Vector2 groupDirection = new Vector2(horizontal, vertical).normalized;

            // Move all selected units in the same direction
            foreach (CellSelectable unit in selectionManager.selectedUnits)
            {
                if (unit != null)
                {
                    // Clear any AI movement target when manually controlling with WASD
                    CellAI cellAI = unit.GetComponent<CellAI>();
                    if (cellAI != null)
                    {
                        cellAI.ClearTarget();
                    }
                    
                    Rigidbody2D rb = unit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = groupDirection * groupMoveSpeed;
                    }
                }
            }
        }
        // If no WASD input, don't interfere with right-click movement
    }

    void ApplyCohesion()
    {
        Vector3 groupCenter = CalculateGroupCenter();

        foreach (CellSelectable unit in selectionManager.selectedUnits)
        {
            if (unit != null)
            {
                // Don't apply cohesion if the unit has an active movement target
                CellAI cellAI = unit.GetComponent<CellAI>();
                if (cellAI != null && cellAI.HasTarget())
                {
                    continue; // Skip cohesion for units with movement targets
                }

                Vector3 toCenter = groupCenter - unit.transform.position;
                float distance = toCenter.magnitude;

                if (distance > 0.5f) // Only if spread out
                {
                    Rigidbody2D rb = unit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 cohesionForce = toCenter.normalized * cohesionStrength;
                        rb.linearVelocity += cohesionForce; // Add to existing velocity
                    }
                }
            }
        }
    }

    Vector3 CalculateGroupCenter()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (CellSelectable unit in selectionManager.selectedUnits)
        {
            if (unit != null)
            {
                center += unit.transform.position;
                count++;
            }
        }

        return count > 0 ? center / count : Vector3.zero;
    }
}