using UnityEngine;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask selectableLayer = -1;
    public RectTransform selectionBox;
    public Canvas uiCanvas;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isDragging = false;
    private bool isRightClickDragging = false;
    private Vector3 rightClickTarget;

    public List<CellSelectable> selectedUnits = new List<CellSelectable>();

    void Start()
    {
        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleMouseInput();
        UpdateSelectionBox();
    }

    void HandleMouseInput()
    {
        // Left click down - start selection
        if (Input.GetMouseButtonDown(0))
        {
            // Get the current active camera (the one that's actually rendering)
            Camera activeCamera = GetActiveCamera();
            if (activeCamera == null)
            {
                Debug.LogError("No active camera found!");
                return;
            }

            Vector3 mousePos = Input.mousePosition;
            Debug.Log("Raw mouse position: " + mousePos);
            Debug.Log("Active camera: " + activeCamera.name + " at position: " + activeCamera.transform.position);

            startPosition = mousePos;

            // Try different Z distances for conversion
            Vector3 worldPos1 = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, activeCamera.nearClipPlane));
            Vector3 worldPos2 = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
            Vector3 worldPos3 = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(activeCamera.transform.position.z)));

            Debug.Log("World pos with nearClipPlane: " + worldPos1);
            Debug.Log("World pos with Z=10: " + worldPos2);
            Debug.Log("World pos with camera Z distance: " + worldPos3);

            Vector3 worldPosition = worldPos3; // Use the camera Z distance
            worldPosition.z = 0;

            Debug.Log("Converted world position: " + worldPosition);

            // Create a visual debug marker at the click position
            CreateClickMarker(worldPosition);

            // Check for collision at the exact world position
            Collider2D hit = Physics2D.OverlapPoint(worldPosition, selectableLayer);
            Debug.Log("Hit object at " + worldPosition + ": " + (hit != null ? hit.name : "nothing"));

            // NEW DEBUG: Show all cells and their positions
            CellSelectable[] allCells = FindObjectsOfType<CellSelectable>();
            Debug.Log("=== All cells in scene ===");
            foreach (CellSelectable cell in allCells)
            {
                Debug.Log("Cell: " + cell.name + " at position: " + cell.transform.position);
                CircleCollider2D[] colliders = cell.GetComponents<CircleCollider2D>();
                foreach (CircleCollider2D col in colliders)
                {
                    Debug.Log("  - Collider: isTrigger=" + col.isTrigger + ", enabled=" + col.enabled);
                }
            }

            if (hit != null)
            {
                Debug.Log("Hit object tag: " + hit.tag);
                Debug.Log("Hit object has CellSelectable: " + (hit.GetComponent<CellSelectable>() != null));
                CellSelectable unit = hit.GetComponent<CellSelectable>();
                if (unit != null)
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                        ClearSelection();

                    SelectUnit(unit);
                    return;
                }
            }

            // No unit hit, start drag selection
            if (!Input.GetKey(KeyCode.LeftShift))
                ClearSelection();

            isDragging = true;
            if (selectionBox != null)
                selectionBox.gameObject.SetActive(true);
        }

        // Rest of the method stays the same...
        // Left click up - end selection
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                PerformDragSelection();
                isDragging = false;
                if (selectionBox != null)
                    selectionBox.gameObject.SetActive(false);
            }
        }

        // Right click - move command
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Camera activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                Vector3 mousePos = Input.mousePosition;
                Vector3 targetPosition = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(activeCamera.transform.position.z)));
                targetPosition.z = 0;  // Force to Z = 0

                rightClickTarget = targetPosition;
                isRightClickDragging = true;
                
                Debug.Log("Right click move command started at: " + targetPosition);
                CreateClickMarker(targetPosition, Color.red);
                MoveSelectedUnits(targetPosition);
            }
        }

        // Right click drag - update move target
        if (isRightClickDragging && selectedUnits.Count > 0)
        {
            Camera activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                Vector3 mousePos = Input.mousePosition;
                Vector3 newTargetPosition = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(activeCamera.transform.position.z)));
                newTargetPosition.z = 0;  // Force to Z = 0

                // Only update if mouse has moved significantly
                if (Vector3.Distance(rightClickTarget, newTargetPosition) > 0.1f)
                {
                    rightClickTarget = newTargetPosition;
                    CreateClickMarker(newTargetPosition, Color.red);
                    MoveSelectedUnits(newTargetPosition);
                }
            }
        }

        // Right click release - end drag
        if (Input.GetMouseButtonUp(1))
        {
            isRightClickDragging = false;
        }

        // Tab key - select all units
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectAllUnits();
        }
    }

    Camera GetActiveCamera()
    {
        // Try multiple ways to get the current camera
        Camera cam = Camera.main;
        if (cam != null && cam.enabled) return cam;

        // If Camera.main fails, find any enabled camera
        Camera[] allCameras = Camera.allCameras;
        foreach (Camera c in allCameras)
        {
            if (c.enabled && c.gameObject.activeInHierarchy)
            {
                Debug.Log("Using fallback camera: " + c.name);
                return c;
            }
        }

        return null;
    }

    void CreateClickMarker(Vector3 worldPos, Color color = default)
    {
        if (color == default) color = Color.green;

        // Create a temporary visual marker to see where we're actually clicking
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ClickMarker";
        marker.transform.position = worldPos;
        marker.transform.localScale = Vector3.one * 0.2f;
        marker.GetComponent<Renderer>().material.color = color;

        // Remove collider so it doesn't interfere
        Destroy(marker.GetComponent<Collider>());

        // Auto-destroy after 2 seconds
        Destroy(marker, 2f);

        Debug.Log("Created click marker at: " + worldPos);
    }

    void UpdateSelectionBox()
    {
        if (!isDragging || selectionBox == null) return;

        endPosition = Input.mousePosition;

        Vector3 center = (startPosition + endPosition) / 2;
        selectionBox.position = center;

        Vector3 size = new Vector3(
            Mathf.Abs(startPosition.x - endPosition.x),
            Mathf.Abs(startPosition.y - endPosition.y),
            0
        );
        selectionBox.sizeDelta = size;
    }

    void PerformDragSelection()
    {
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;

        Vector3 min = Vector3.Min(startPosition, endPosition);
        Vector3 max = Vector3.Max(startPosition, endPosition);

        CellSelectable[] allUnits = FindObjectsOfType<CellSelectable>();

        foreach (CellSelectable unit in allUnits)
        {
            Vector3 screenPos = activeCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPos.x >= min.x && screenPos.x <= max.x &&
                screenPos.y >= min.y && screenPos.y <= max.y)
            {
                SelectUnit(unit);
            }
        }
    }

    void SelectUnit(CellSelectable unit)
    {
        // Don't select enemy cells
        if (unit.GetComponent<EnemyCell>() != null)
        {
            return;
        }
        
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }
    }

    void ClearSelection()
    {
        foreach (CellSelectable unit in selectedUnits)
        {
            if (unit != null)
                unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }

    void SelectAllUnits()
    {
        ClearSelection();

        CellSelectable[] allUnits = FindObjectsOfType<CellSelectable>();
        foreach (CellSelectable unit in allUnits)
        {
            if (unit != null && unit.GetComponent<EnemyCell>() == null)
            {
                SelectUnit(unit);
            }
        }
    }

    void MoveSelectedUnits(Vector3 targetPosition)
    {
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            if (selectedUnits[i] != null)
            {
                Vector3 offset = Random.insideUnitCircle * 0.5f * selectedUnits.Count;
                Vector3 finalTarget = targetPosition + offset;
                selectedUnits[i].MoveToPosition(finalTarget);
            }
        }
    }
}