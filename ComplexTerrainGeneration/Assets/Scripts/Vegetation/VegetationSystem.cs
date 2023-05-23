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

    public float plantRandomness = 1f;

    public float distributionRandomness = 0.15f;
    public AnimationCurve treeDistributionCurve;
    
    private List<GameObject> spawnedPlants;

    public void Vegetate(float[,] heightMap, float heightScalar, Vector3 chunkPosition, float plantBounds = 0f)
    {
        AnimationCurve treeDistCurve = new AnimationCurve(treeDistributionCurve.keys);

        if (plantBounds == 0f)
            plantBounds = plantRandomness;
        
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

                // Selects the type of plant to spawn based on the height and a random
                // offset to give a smooth blend between different forest types
                float plantType = (basePosition.y / heightScalar) * (maxPlantHeight - minPlantHeight);
                plantType += Random.Range(-distributionRandomness, distributionRandomness);
                int plantTypeFinal = (int)treeDistCurve.Evaluate(plantType);

                // Spawns in a plant if the height is within the min and max height range for the plant
                if (basePosition.y < maxPlantHeight && basePosition.y > minPlantHeight)
                    spawnedPlants.Add(Instantiate(plants[plantTypeFinal].prefab, basePosition, Quaternion.identity));
            }
        }
    }

    private Vector3 FindPosition(Vector3 startPos, float spawnRad, Plant plant)
    {
        Vector3 position = startPos;
        
        int iter = 0;
        //while ((position.y > plant.maxHeight || position.y < plant.minHeight) && iter < maxAttempts)
        //{
        //    iter++;
        //    position = startPos; 
        //            
        //    position.x += Random.Range(-(spawnRad / 2f), spawnRad / 2f);
        //    position.z += Random.Range(-(spawnRad / 2f), spawnRad / 2f);
        //        
        //    RaycastHit hit;
        //    if (Physics.Raycast(position, Vector3.down, out hit, 100 - plant.minHeight))
        //    {
        //        position.y = hit.point.y;
        //    }
        //}

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
