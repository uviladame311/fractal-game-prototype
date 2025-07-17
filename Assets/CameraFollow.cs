using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Group Camera Settings")]
    public float baseZoom = 5f;           // Base camera size
    public float zoomPerUnit = 1f;        // Additional zoom per selected unit
    public float maxZoom = 25f;           // Maximum zoom out (increased for large cells)
    public float spreadMultiplier = 2f;   // How much spread affects zoom
    public float sizeZoomMultiplier = 1.5f; // How much cell size affects zoom

    private Camera cam;
    private SelectionManager selectionManager;

    void Start()
    {
        cam = GetComponent<Camera>();
        selectionManager = FindObjectOfType<SelectionManager>();
    }

    void LateUpdate()
    {
        if (selectionManager != null && selectionManager.selectedUnits.Count > 0)
        {
            // Calculate average position of selected units
            Vector3 centerPosition = CalculateGroupCenter();

            // Calculate desired camera position
            Vector3 desiredPosition = centerPosition + offset;

            // Smoothly move toward the group center
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Adjust camera zoom based on group size and spread
            AdjustCameraZoom();
        }
    }

    Vector3 CalculateGroupCenter()
    {
        Vector3 center = Vector3.zero;
        int validUnits = 0;

        foreach (CellSelectable unit in selectionManager.selectedUnits)
        {
            if (unit != null)
            {
                center += unit.transform.position;
                validUnits++;
            }
        }

        if (validUnits > 0)
            center /= validUnits;

        return center;
    }

    void AdjustCameraZoom()
    {
        if (cam == null) return;

        int unitCount = selectionManager.selectedUnits.Count;

        // Calculate spread of units
        float maxDistance = 0f;
        float averageCellSize = 0f;
        Vector3 center = CalculateGroupCenter();
        int validUnits = 0;

        foreach (CellSelectable unit in selectionManager.selectedUnits)
        {
            if (unit != null)
            {
                float distance = Vector3.Distance(center, unit.transform.position);
                if (distance > maxDistance)
                    maxDistance = distance;

                // Get cell size for zoom calculation
                CellController cellController = unit.GetComponent<CellController>();
                if (cellController != null)
                {
                    averageCellSize += cellController.currentSize;
                    validUnits++;
                }
            }
        }

        if (validUnits > 0)
            averageCellSize /= validUnits;

        // Calculate desired zoom including cell size
        float desiredZoom = baseZoom 
            + (unitCount * zoomPerUnit) 
            + (maxDistance * spreadMultiplier)
            + (averageCellSize * sizeZoomMultiplier); // New: zoom based on cell size

        desiredZoom = Mathf.Clamp(desiredZoom, baseZoom, maxZoom);

        // Smoothly adjust camera size
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredZoom, smoothSpeed);
    }
}