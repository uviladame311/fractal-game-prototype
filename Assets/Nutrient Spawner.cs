using UnityEngine;

public class NutrientSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject nutrientPrefab;      // Drag your nutrient prefab here
    public float spawnRate = 5f;           // Nutrients per second (increased from 2)
    public float spawnRadius = 15f;        // How far from center to spawn (increased from 10)
    public int maxNutrients = 50;          // Maximum nutrients on screen (increased from 20)

    private float nextSpawnTime = 0f;
    private int currentNutrientCount = 0;

    void Update()
    {
        // Check if it's time to spawn and we haven't hit the max
        if (Time.time >= nextSpawnTime && currentNutrientCount < maxNutrients)
        {
            SpawnNutrient();
            nextSpawnTime = Time.time + (1f / spawnRate);
        }
    }

    void SpawnNutrient()
    {
        // Generate random position within spawn radius
        Vector2 randomPosition = Random.insideUnitCircle * spawnRadius;

        // Spawn the nutrient
        GameObject newNutrient = Instantiate(nutrientPrefab, randomPosition, Quaternion.identity);

        // Listen for when this nutrient gets destroyed
        Nutrient nutrientScript = newNutrient.GetComponent<Nutrient>();
        if (nutrientScript != null)
        {
            // We'll handle counting later
        }

        currentNutrientCount++;
    }

    // Call this when a nutrient is eaten
    public void NutrientEaten()
    {
        currentNutrientCount--;
    }
}