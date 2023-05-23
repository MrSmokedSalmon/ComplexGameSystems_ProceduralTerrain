using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class VegetationSystem : MonoBehaviour
{
    public Plant[] plants;

    public float maxPlantHeight;
    public float minPlantHeight;

    public int targetDensity = 3;
    
    public float plantRandomness = 1f;

    public float distributionRandomness = 0.15f;
    public AnimationCurve treeDistributionCurve;
    
    private List<GameObject> spawnedPlants;

    public void Vegetate(float[,] heightMap, float heightScalar, Vector3 chunkPosition, float plantBounds = 0f)
    {
        AnimationCurve treeDistCurve = new AnimationCurve(treeDistributionCurve.keys);

        if (plantBounds == 0f)
            plantBounds = plantRandomness;
        
        // Gets the width and the height and ensures they are an odd number
        int width = heightMap.GetLength(0);
        width -= width % 2 == 0 ? 1 : 0;
        int height = heightMap.GetLength(1);
        height -= height % 2 == 0 ? 1 : 0;

        // Gets an offset to skip every set number of vertices, with a target of density
        int skipX = 1;
        for (int x = targetDensity; x < targetDensity + 11; x++)
        {
            if (width % x == 0)
            {
                skipX = x;
                x = targetDensity + 11;
            }
        }
        int skipY = 1;
        for (int y = targetDensity; y < targetDensity + 11; y++)
        {
            if (height % y == 0)
            {
                skipY = y;
                y = targetDensity + 11;
            }
        }

        width /= 2;
        height /= 2;

        Vector3 basePosition = chunkPosition;
        
        for (int x = -width; x < width; x += skipX)
        {
            for (int y = -height; y < height; y += skipY)
            {
                basePosition = chunkPosition;

                // Sets the spawn position to the vertex position + a slight random offset to break up the grid
                basePosition.x += x + Random.Range(-plantBounds, plantBounds);
                basePosition.y = heightMap[height - y, width + x] * heightScalar;
                basePosition.z += y + Random.Range(-plantBounds, plantBounds);

                // Selects the type of plant to spawn based on the height and a random
                // offset to give a smooth blend between different forest types
                float plantType = (basePosition.y / heightScalar) * (maxPlantHeight - minPlantHeight);
                plantType += Random.Range(-distributionRandomness, distributionRandomness);
                int plantTypeFinal = (int)treeDistCurve.Evaluate(plantType);

                // Spawns in a plant if the height is within the min and max height range for the plant
                if (basePosition.y < maxPlantHeight && basePosition.y > minPlantHeight)
                    spawnedPlants.Add(Instantiate(plants[plantTypeFinal].prefab, basePosition, 
                        Quaternion.Euler(0, Random.Range(0f, 360f), 0)));
            }
        }
    }

    public void Devegetate()
    {
        foreach (var plant in spawnedPlants) 
        {
#if UNITY_EDITOR
            DestroyImmediate(plant);
#else
            Destroy(plant);
#endif
        }
        spawnedPlants.Clear();
    }
}
