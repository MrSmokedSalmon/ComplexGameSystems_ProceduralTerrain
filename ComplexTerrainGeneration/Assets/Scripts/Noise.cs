using TreeEditor;
using UnityEngine;



public static class Noise
{
    private const float symmetryOffsetX = 100000f;
    private const float symmetryOffsetY = 100000f;
    
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 octaveOffset, Vector2 offest)
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

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1.0f;
                float frequency = 1.0f;
                float noiseHeight = 0;
                
                //float sampleX = (x + offest.x) / scale;
                //float sampleY = (y + offest.y) / scale;
                //
                //float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                //noiseHeight += perlinValue;
                
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + symmetryOffsetX + offest.y) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y + symmetryOffsetY + offest.x) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    if(perlinValue < 0f)
                        sampleX++;
                    perlinValue = perlinValue * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;
                noiseMap[y, x] = noiseHeight;
            }

        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                noiseMap[y, x] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[y, x]);

        return noiseMap;
    }
}