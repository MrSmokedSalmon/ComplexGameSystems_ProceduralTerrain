using UnityEngine;

public class NewNoise : MonoBehaviour
{
    public static float[,] GenerateNoiseMap(int mapLength, int mapWidth, float scale, Vector2 offset, int octaves, float persistance, float lacunarity, int seed)
    {
        float[,] noiseMap = new float[mapLength, mapWidth];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        for (int z = 0; z < mapLength; z++) {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - offset.y) / scale * frequency + octaveOffsets[i].x;
                    float sampleZ = (z + offset.x) / scale * frequency + octaveOffsets[i].y;

                    float perlinVal = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;

                    noiseHeight += perlinVal * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                
                noiseMap[z, x] = noiseHeight;
            }
        }

        return noiseMap;
    }
}