using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class VegetationSystem : MonoBehaviour
{
    public Plant[] plants;

    public int maxPlants;
    public int minPlants;

    public AnimationCurve treeDistributionCurve;
    
    [Min(1)] public int maxAttempts;
    
    private List<GameObject> spawnedPlants;

    public void Vegetate(float[,] heightMap, float heightScalar, Vector3 chunkPosition, float plantBounds, int numPlants)
    {
        AnimationCurve treeDistCurve = new AnimationCurve(treeDistributionCurve.keys);
        
        int plantTypes = plants.Length;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Gets an offset to skip every couple of vertices
        int skipX = 1;
        for (int x = 3; x < 11; x++)
        {
            if (width % x == 0)
            {
                skipX = x;
                x = 11;
            }
        }
        int skipY = 1;
        for (int y = 3; y < 11; y++)
        {
            if (height % y == 0)
            {
                skipY = y;
                y = 11;
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

                float plantType = basePosition.y / heightScalar;
                plantType += Random.Range(-0.15f, 0.15f);
                int plantTypeFinal;
                
                // Spawns in a plant if the height is within the min and max height range for the plant
                if (basePosition.y < plants[0].maxHeight && basePosition.y > plants[0].minHeight)
                    spawnedPlants.Add(Instantiate(plants[0].prefab, basePosition, Quaternion.identity));
            }
        }
    }

    private Vector3 FindPosition(Vector3 startPos, float spawnRad, Plant plant)
    {
        Vector3 position = startPos;
        
        int iter = 0;
        while ((position.y > plant.maxHeight || position.y < plant.minHeight) && iter < maxAttempts)
        {
            iter++;
            position = startPos; 
                    
            position.x += Random.Range(-(spawnRad / 2f), spawnRad / 2f);
            position.z += Random.Range(-(spawnRad / 2f), spawnRad / 2f);
                
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, 100 - plant.minHeight))
            {
                position.y = hit.point.y;
            }
        }

        return position;
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
