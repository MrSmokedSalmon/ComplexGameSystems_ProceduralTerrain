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

                    float perlinValue = perlin(sampleX, sampleY);
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
        Vector2 offest, int octaves, float persistance, float lacunarity, int seed, AnimationCurve _curve, float curveOffset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        AnimationCurve curve = new AnimationCurve(_curve.keys);
        
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
                float sampleX = (x + symmetryOffsetX + offest.x) / scale * frequency + octaveOffsets[i].x;
                float sampleY = (y + symmetryOffsetY - offest.y) / scale * frequency + octaveOffsets[i].y;

                float perlinValue = perlin(sampleX, sampleY);
                if(perlinValue < 0f)
                    sampleX++;
                perlinValue = perlinValue * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            float val = curve.Evaluate(noiseHeight + curveOffset / 100f);
            noiseMap[y, x] = val;
        }

        return noiseMap;
    }
    
    // Wikipedia Implementation of Perlin Noise
    public static float Interpolate(float a0, float a1, float w)
    {
        return (a1 - a0) * w + a0;
        //return (a1 - a0) * ((w * (w * 6.0f - 15.0f) + 10.0f) * w * w * w) + a0;
    }

    public static Vector2 RandomGradient(int ix, int iy)
    {
        const byte w = 8 * sizeof(uint);
        const byte s = w / 2;
        
        uint a = (uint)ix;
        uint b = (uint)iy;

        a *= 3284157443; b ^= a << s | a >> w - s;
        b *= 1911520717; a ^= b << s | b >> w - s;
        a *= 2048419325;

        float random = a * (3.14159265f / ~(~0u >> 1));
        Vector2 v;
        v.x = Mathf.Cos(random);
        v.y = Mathf.Sin(random);
        return v;
    }

    public static float DotGridGradient(int ix, int iy, float x, float y)
    {
        Vector2 gradient = RandomGradient(ix, iy);

        float dx = x - (float)ix;
        float dy = y - (float)iy;

        return (dx * gradient.x + dy * gradient.y);
    }

    public static float perlin(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int x1 = x0 + 1;
        int y0 = Mathf.FloorToInt(y);
        int y1 = y0 + 1;

        float sx = x - (float)x0;
        float sy = y - (float)y0;

        float n0, n1, ix0, ix1, value;

        n0 = DotGridGradient(x0, y0, x, y);
        n1 = DotGridGradient(x1, y0, x, y);
        ix0 = Interpolate(n0, n1, sx);
        
        n0 = DotGridGradient(x0, y1, x, y);
        n1 = DotGridGradient(x1, y1, x, y);
        ix1 = Interpolate(n0, n1, sx);

        value = Interpolate(ix0, ix1, sy);
        return value * 0.5f + 0.5f;
    }
}