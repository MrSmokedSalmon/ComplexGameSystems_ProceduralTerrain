using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class HeightMapGen : MonoBehaviour
{
    public enum DrawMode
    {
        NosieMap,
        ColourMap,
        BiomeMap,
        HeatMap,
        HumidityMap,
        HeightMask,
        Mesh
    };

    public DrawMode drawMode;

    public const int mapChunkSize = 129;
    [Range(0,6)]public int levelOfDetailEditor;
    [Min(0.001f)]public float scale;
    [Min(1)]public int octaves;
    [Range(-1, 2)]public float persistance;
    [Range(-2, 5)]public float lacunarity;
    public int seed;
    public Vector2 octaveOffset;
    public Vector2 positionOffset;
    
    public float heightMulti;
    public AnimationCurve heightCurve;
    
    public bool autoUpdate;
    public bool usePositionAsOffset;
    
    public TerrainTypes[] regions;
    public BiomeTypes[] biomes;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? new Vector2(transform.position.x, transform.position.z) : positionOffset, 
            octaves, persistance, lacunarity, seed);
        
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        Color[] biomeMap = new Color[mapChunkSize * mapChunkSize];
        
        for (int y = 0; y < mapChunkSize; y++)
            for (int x = 0; x < mapChunkSize; x++)
            {
                //noiseMap[x, y] *= heightMask[x, y];
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                for (int i = 0; i < biomes.Length; i++)
                    if (currentHeight <= biomes[i].value)
                    {
                        biomeMap[y * mapChunkSize + x] = biomes[i].color;
                        break;
                    }
            }

        return new MapData(noiseMap, colorMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        
        MapDisplay display = GetComponent<MapDisplay>();
        
        if (drawMode == DrawMode.NosieMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        //else if (drawMode == DrawMode.BiomeMap)
        //    display.DrawTexture(TextureGen.TextureFromColorMap(biomeMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.HeatMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(GenerateHeatMap()));
        else if (drawMode == DrawMode.HumidityMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(GenerateHumidityMap()));
        else if (drawMode == DrawMode.HeightMask)
            display.DrawTexture(TextureGen.TextureFromHeightMap(GenerateHeightMask()));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
    }
    
    public float[,] GenerateHeatMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? new Vector2(transform.position.x, transform.position.z) : positionOffset, 
            octaves, persistance, lacunarity, seed);
        return noiseMap;
    }

    public float[,] GenerateHumidityMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? new Vector2(transform.position.x, transform.position.z) : positionOffset, 
            octaves, persistance, lacunarity, seed);
        return noiseMap;
    }
    
    public float[,] GenerateHeightMask()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? new Vector2(transform.position.x, transform.position.z) : positionOffset, 
            octaves, persistance, lacunarity, seed);
        return noiseMap;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> _callback, T _parameter)
        {
            callback = _callback;
            parameter = _parameter;
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

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] _heightMap, Color[] _colorMap)
    {
        heightMap = _heightMap;
        colorMap = _colorMap;
    }
}