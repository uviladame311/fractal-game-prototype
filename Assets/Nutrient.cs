using UnityEngine;

public class Nutrient : MonoBehaviour
{
    [Header("Nutrient Settings")]
    public float nutritionValue = 1f;
    public Color nutrientColor = Color.green;

    void Start()
    {
        GetComponent<SpriteRenderer>().color = nutrientColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        CellController cell = other.GetComponent<CellController>();
        if (cell != null)
        {
            cell.EatNutrient(nutritionValue);

            // Tell the spawner this nutrient was eaten
            NutrientSpawner spawner = FindObjectOfType<NutrientSpawner>();
            if (spawner != null)
            {
                spawner.NutrientEaten();
            }

            Destroy(gameObject);
        }
    }
}