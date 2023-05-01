using System;
using UnityEngine;

public class HeightMapGen : MonoBehaviour
{
    public enum DrawMode
    {
        NosieMap,
        ColourMap,
        BiomeMap,
        Mesh
    };

    public DrawMode drawMode;
    
    [Min(1)]public int mapWidth;
    [Min(1)]public int mapHeight;
    [Min(0.00001f)]public float scale;
    [Min(1)]public int octaves;
    [Range(-1, 2)]public float persistance;
    [Range(-2, 5)]public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float heightScale;
    
    
    public TerrainTypes[] regions;
    public BiomeTypes[] biomes;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, scale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapWidth * mapHeight];
        
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
            }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        
        if (drawMode == DrawMode.NosieMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGen.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(noiseMap, heightScale, regions[0].height),
                TextureGen.TextureFromColorMap(colorMap, mapWidth, mapHeight));
    }

    public void GenerateBiomes()
    {
        Color[] colors = new Color[mapWidth * mapHeight];
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, scale, octaves, persistance, lacunarity, offset);
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                Color biomeColor = biomes[0].color;
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (noiseMap[x, y] > biomes[i].value)
                    {
                        colors[y * mapWidth + x] = biomeColor;
                        break;
                    }
                }
            }
        
    }
}
[Serializable]
public struct TerrainTypes
{
    public float height;
    public Color color;
    public string name;
}
[Serializable]
public struct BiomeTypes
{
    public float value;
    public Color color;
    public string name;
}