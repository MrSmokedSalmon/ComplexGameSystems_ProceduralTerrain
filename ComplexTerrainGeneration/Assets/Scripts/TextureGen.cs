using System;
using UnityEngine;

public static class TextureGen
{
    public static Texture2D TextureFromColorMap(Color[] colormap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(colormap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x,y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }
    
    public static Texture2D TextureFromHeightMap(float[,] heightMap, AnimationCurve curve)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, curve.Evaluate(heightMap[x,y]));
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

}