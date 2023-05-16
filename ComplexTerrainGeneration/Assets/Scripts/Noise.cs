using TreeEditor;
using UnityEngine;



public static class Noise
{
    private const float symmetryOffsetX = 100000f;
    private const float symmetryOffsetY = 100000f;
    
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, Vector2 octaveOffset, 
        Vector2 offest, int octaves, float persistance, float lacunarity, int seed)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + octaveOffset.x;
            float offsetY = prng.Next(-100000, 100000) + octaveOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        if (scale <= 0)
            scale = 0.00001f;

        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                // Intiating noise values for this coordinate
                float amplitude = 1.0f;
                float frequency = 1.0f;
                float noiseHeight = 0;

                // Loop through 
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + symmetryOffsetX - offest.y) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y + symmetryOffsetY + offest.x) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    if(perlinValue < 0f)
                        sampleX++;
                    perlinValue = perlinValue * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                
                noiseMap[y, x] = noiseHeight;
            }

        return noiseMap;
    }
    
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, Vector2 octaveOffset, 
        Vector2 offest, int octaves, float persistance, float lacunarity, int seed, AnimationCurve curve)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + octaveOffset.x;
            float offsetY = prng.Next(-100000, 100000) + octaveOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        if (scale <= 0)
            scale = 0.00001f;

        for (int y = 0; y < mapHeight; y++)
        for (int x = 0; x < mapWidth; x++)
        {
            // Initiating noise values for this coordinate
            float amplitude = 1.0f;
            float frequency = 1.0f;
            float noiseHeight = 0;

            // Loop through 
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + symmetryOffsetX - offest.y) / scale * frequency + octaveOffsets[i].x;
                float sampleY = (y + symmetryOffsetY + offest.x) / scale * frequency + octaveOffsets[i].y;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                if(perlinValue < 0f)
                    sampleX++;
                perlinValue = perlinValue * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            float val = curve.Evaluate(noiseHeight);
            noiseMap[y, x] = val;
        }

        return noiseMap;
    }
}