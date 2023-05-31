using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class VegetationSystem : MonoBehaviour
{
    public static VegetationSystem instance;

    public Plant[] plants;

    public float maxPlantHeight;
    public float minPlantHeight;

    public int targetDensity = 3;

    public float plantRandomness = 1f;

    public int vegetationSeed = 0;

    public float distributionRandomness = 0.15f;
    public AnimationCurve treeDistributionCurve;
    
    private List<GameObject> spawnedPlants;

    private void OnEnable()
    {
        if (instance == null)
            instance = this;
        
        maxPlantHeight /= 100;
        minPlantHeight /= 100;
    }
    

    public void Vegetate(float[,] heightMap, float heightScalar, Vector3 chunkPosition, float plantBounds = 0f)
    {
        if (plantBounds == 0f)
            plantBounds = instance.plantRandomness;

        // Gets the width and the height and ensures they are an odd number
        int width = heightMap.GetLength(0);
        width -= width % 2 == 0 ? 1 : 0;
        int height = heightMap.GetLength(1);
        height -= height % 2 == 0 ? 1 : 0;

        // Generates an array of Plant Data
        PlantData[] plantData = GenerateChunkPlants(width, height, heightMap, chunkPosition, heightScalar, plantBounds);

        // Spawns in the vegetation using the Planta Data just created
        foreach (PlantData plant in plantData)
        {
            // Spawns in a plant if the height is within the min and max height range for the plant
            if (plant.position.y < instance.maxPlantHeight && plant.position.y > instance.minPlantHeight)
                spawnedPlants.Add(Instantiate(instance.plants[plant.plantType].prefab, plant.position,
                    plant.rotation));
        }
    }

    public static List<GameObject> SpawnPlants(PlantData[] plantDatas)
    {
        List<GameObject> plantObjects = new List<GameObject>();

        foreach (PlantData plant in plantDatas)
        {
            plantObjects.Add(Instantiate(instance.plants[plant.plantType].prefab, plant.position, plant.rotation));
        }

        return plantObjects;
    }

    public static PlantData GeneratePlant(int offX, int offZ, float height, Vector3 initialPos, float hScalar, float boundary, int rngSeed)
    {
        System.Random rng = new System.Random(instance.vegetationSeed + (int)initialPos.x * 100 + offX - offZ + rngSeed);

        AnimationCurve treeDistCurve = new AnimationCurve(instance.treeDistributionCurve.keys);

        PlantData newPlant = new PlantData();

        Vector3 basePosition = initialPos;

        // Sets the spawn position to the vertex position + a slight random offset to break up the grid
        basePosition.x += offX + (float)rng.NextDouble() * (boundary - (-boundary)) + (-boundary);
        basePosition.y = height * hScalar;
        basePosition.z += offZ + (float)rng.NextDouble() * (boundary - (-boundary)) + (-boundary);

        // Selects the type of plant to spawn based on the height and a random
        // offset to give a smooth blend between different forest types
        float plantType = height * (instance.maxPlantHeight - instance.minPlantHeight);
        plantType = map(plantType, instance.minPlantHeight, instance.maxPlantHeight, 0f, 1f);
        plantType += (float)rng.NextDouble() * instance.distributionRandomness;
        
        int plantTypeFinal = (int)treeDistCurve.Evaluate(plantType);

        // Sets the plant data
        newPlant.position = basePosition;
        newPlant.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
        newPlant.plantType = plantTypeFinal;

        return newPlant;
    }
    
    // found at: https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/
    public static float map (float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static PlantData[] GenerateChunkPlants(int width, int height, float[,] heightMap, Vector3 chunkPosition, float heightScalar, float plantBounds = 0f)
    {
        if (plantBounds == 0f)
            plantBounds = instance.plantRandomness;

        List<PlantData> plantDatas = new List<PlantData>();
        
        System.Random seedRNG = new System.Random(instance.vegetationSeed);
        
        for (int x = 0; x < width; x += instance.targetDensity)
        {
            for (int y = 0; y < height; y += instance.targetDensity)
            {
                if (heightMap[height - y, x] > instance.maxPlantHeight || heightMap[height - y, x] < instance.minPlantHeight)
                    continue;
                
                // Generates a new plant
                PlantData plant = GeneratePlant(x - (width / 2), y - (height / 2), heightMap[height - y, x],
                    chunkPosition, heightScalar, plantBounds, seedRNG.Next(-999999, 999999));

                plantDatas.Add(plant);
            }
        }

        return plantDatas.ToArray();
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