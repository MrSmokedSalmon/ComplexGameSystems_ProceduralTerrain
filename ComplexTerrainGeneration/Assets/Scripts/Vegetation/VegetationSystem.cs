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
        //int skipX = 1;
        //for (int x = targetDensity; x < targetDensity + 11; x++)
        //{
        //    if (width % x == 0)
        //    {
        //        skipX = x;
        //        x = targetDensity + 11;
        //    }
        //}
        //int skipY = 1;
        //for (int y = targetDensity; y < targetDensity + 11; y++)
        //{
        //    if (height % y == 0)
        //    {
        //        skipY = y;
        //        y = targetDensity + 11;
        //    }
        //}

        PlantData[] plantData = GenerateChunkPlants(width, height, heightMap, chunkPosition, plantBounds, heightScalar);

        foreach (PlantData plant in plantData)
        {
            // Spawns in a plant if the height is within the min and max height range for the plant
            if (plant.position.y < maxPlantHeight && plant.position.y > minPlantHeight)
                spawnedPlants.Add(Instantiate(plants[plant.plantType].prefab, plant.position,
                    plant.rotation));
        }
        
        //for (int x = 0; x < width; x += targetDensity)
        //{
        //    for (int y = 0; y < height; y += targetDensity)
        //    {
        //        // Generates a new plant
        //        PlantData plant = GeneratePlant(x - (width / 2), y - (height / 2), heightMap[height - y, x],
        //            chunkPosition, plantBounds, heightScalar);
        //        
        //        // Spawns in a plant if the height is within the min and max height range for the plant
        //        if (plant.position.y < maxPlantHeight && plant.position.y > minPlantHeight)
        //            spawnedPlants.Add(Instantiate(plants[plant.plantType].prefab, plant.position,
        //                plant.rotation));
        //    }
        //}
        
        //width /= 2;
        //height /= 2;
//
        //for (int x = -width; x < width; x += targetDensity)
        //{
        //    for (int y = -height; y < height; y += targetDensity)
        //    {
        //        // Generates a new plant
        //        PlantData plant = GeneratePlant(x, y, heightMap[height - y, width + x],
        //            chunkPosition, plantBounds, heightScalar);
//
        //        // Spawns in a plant if the height is within the min and max height range for the plant
        //        if (plant.position.y < maxPlantHeight && plant.position.y > minPlantHeight)
        //            spawnedPlants.Add(Instantiate(plants[plant.plantType].prefab, plant.position,
        //                plant.rotation));
        //    }
        //}
    }

    public PlantData GeneratePlant(int offX, int offZ, float height, Vector3 initialPos, float boundary, float hScalar)
    {
        AnimationCurve treeDistCurve = new AnimationCurve(treeDistributionCurve.keys);

        PlantData newPlant = new PlantData();

        Vector3 basePosition = initialPos;

        // Sets the spawn position to the vertex position + a slight random offset to break up the grid
        basePosition.x += offX + Random.Range(-boundary, boundary);
        basePosition.y = height * hScalar;
        basePosition.z += offZ + Random.Range(-boundary, boundary);

        // Selects the type of plant to spawn based on the height and a random
        // offset to give a smooth blend between different forest types
        float plantType = height * (maxPlantHeight - minPlantHeight);
        plantType += Random.Range(-distributionRandomness, distributionRandomness);
        int plantTypeFinal = (int)treeDistCurve.Evaluate(plantType);

        // Sets the plant data
        newPlant.position = basePosition;
        newPlant.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        newPlant.plantType = plantTypeFinal;

        return newPlant;
    }

    public PlantData[] GenerateChunkPlants(int width, int height, float[,] heightMap, Vector3 chunkPosition, float plantBounds, float heightScalar)
    {
        PlantData[] plants = new PlantData[(width * 2) * (height * 2)];
        
        for (int x = 0; x < width; x += targetDensity)
        {
            for (int y = 0; y < height; y += targetDensity)
            {
                // Generates a new plant
                PlantData plant = GeneratePlant(x - (width / 2), y - (height / 2), heightMap[height - y, x],
                    chunkPosition, plantBounds, heightScalar);

                plants[y * height + x] = plant;
            }
        }

        return plants;
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

public struct PlantData
{
    public Vector3 position;
    public Quaternion rotation;
    public int plantType;
}