using UnityEngine;

public class CellSelectable : MonoBehaviour
{
    [Header("Selection Visual")]
    public GameObject selectionRing;
    public Color selectedColor = Color.yellow;

    private bool isSelected = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private CellController cellController;
    private CellAI cellAI;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cellController = GetComponent<CellController>();
        cellAI = GetComponent<CellAI>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        CreateSelectionRing();
    }

    void CreateSelectionRing()
    {
        if (selectionRing == null)
        {
            Debug.Log("Creating selection ring for " + gameObject.name);
            selectionRing = new GameObject("SelectionRing");
            selectionRing.transform.SetParent(transform);
            selectionRing.transform.localPosition = Vector3.zero;

            SpriteRenderer ringRenderer = selectionRing.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = spriteRenderer.sprite; // Use same sprite
            ringRenderer.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f);
            ringRenderer.sortingOrder = -1;

            selectionRing.transform.localScale = Vector3.one * 1.2f; // Slightly bigger
            selectionRing.SetActive(false);
            Debug.Log("Selection ring created and set to inactive");
        }
    }

    public void SetSelected(bool selected)
    {
        Debug.Log("SetSelected called on " + gameObject.name + " with value: " + selected);
        isSelected = selected;

        if (selectionRing != null)
        {
            Debug.Log("Setting selection ring active: " + selected);
            selectionRing.SetActive(selected);
        }
        else
        {
            Debug.Log("Selection ring is null!");
        }

        // Keep AI enabled but it will check isPlayerControlled internally
        if (cellAI != null)
        {
            Debug.Log("CellAI remains enabled, player control: " + selected);
        }

        // Enable/disable direct control
        if (cellController != null)
        {
            cellController.isPlayerControlled = selected;
            Debug.Log("Player control enabled: " + selected);
        }
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        Debug.Log("Move command received for " + gameObject.name + " to " + targetPosition);
        if (cellAI != null)
        {
            cellAI.SetTarget(targetPosition);
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }
}