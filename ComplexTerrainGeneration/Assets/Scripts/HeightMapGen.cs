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
        ColourMap,
        HeatMap,
        HumidityMap,
        DetailMap,
        ContinentMap,
        HeightMap,
        Continent,
        Details,
        Final
    };

    public DrawMode drawMode;

    public const int mapChunkSize = 130;
    [Range(0,6)]public int levelOfDetailEditor;
    [Min(0.001f)]public float scale;
    [Min(1)]public int octaves;
    [Range(-1, 2)]public float persistance;
    [Range(-2, 5)]public float lacunarity;
    public int seed;
    public Vector2 octaveOffset;
    public Vector2 positionOffset;
    public float continentSize = 0.0f;
    
    public float heightMulti;
    public AnimationCurve heightCurve;
    public AnimationCurve detailCurve;
    public AnimationCurve continentCurve;
    public AnimationCurve moistureCurve;

    public VegetationSystem vegeSystem;
    
    public bool autoUpdate;
    public bool usePositionAsOffset;

    public MapData chunkData;
    
    public TerrainTypes[] regions;
    public BiomeTypes[] biomes;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMapData(Action<MapData> callback, Vector2 position)
    {
        //MapDataThread(callback, position);
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback, position);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback, Vector2 position)
    {
        MapData mapData = GenerateMapData(position);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        //MeshDataThread(mapData, lod, callback, drawMode);
        
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback, drawMode);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback, DrawMode dMode)
    {
        MeshData meshData;
        
        if (dMode == DrawMode.Continent)
            meshData = MeshGen.GenerateTerrainMesh(mapData.continentMap, 10, continentCurve, lod);
        else if (dMode == DrawMode.Details)
            meshData = MeshGen.GenerateTerrainMesh(mapData.detailMap, heightMulti, heightCurve, lod);
        else
            meshData = MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, lod);
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

    private MapData GenerateMapData(Vector2 _position)
    {
        float[,] heightMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? _position : positionOffset, 
            octaves, persistance, lacunarity, seed, detailCurve, 0f);
        float[,] continentMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale * 10.0f, 
            octaveOffset, usePositionAsOffset ? _position : positionOffset, 
            6, persistance, lacunarity, seed, continentCurve, continentSize);
        float[,] finalMap = CombineNoiseMaps(heightMap, continentMap, 4, heightCurve);
        
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        
        for (int y = 0; y < mapChunkSize; y++)
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = finalMap[y,x];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
            }

        chunkData = new MapData(heightMap, continentMap, finalMap, colorMap);
        return chunkData;
    }

    private float[,] CombineNoiseMaps(float[,] map1, float[,] map2, uint opperation)
    {
        int width = map1.GetLength(0);
        int height = map1.GetLength(1);
        
        if (width != map2.GetLength(0) ||
            height != map2.GetLength(1))
            return null;

        float[,] newNoise = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = 0f;
                switch (opperation)
                {
                    case 0:
                        newNoise[y,x] = map1[y,x] + map2[y,x];
                        break;
                    case 1:
                        newNoise[y,x] = map1[y,x] - map2[y,x];
                        break;
                    case 2:
                        newNoise[y,x] = map1[y,x] * map2[y,x];
                        break;
                    case 3:
                        newNoise[y,x] = map1[y,x] / map2[y,x];
                        break;
                    case 4:
                        value = (map1[y,x] * map2[y,x] + Mathf.Abs(map1[y,x] * map2[y,x])) / 2f;
                        newNoise[y,x] = value + (0.006f * map2[y,x]);
                        break;
                    case 5:
                        value = (map1[y,x] / map2[y,x] + Mathf.Abs(map1[y,x] * map2[y,x])) / 2f;
                        newNoise[y,x] = value;
                        break;
                    default:
                        break;
                }
            }
        }

        return newNoise;
    }

    private float[,] CombineNoiseMaps(float[,] map1, float[,] map2, uint opperation, AnimationCurve _curve)
    {
        AnimationCurve curve = new AnimationCurve(_curve.keys);
        
        int width = map1.GetLength(0);
        int height = map1.GetLength(1);
        
        if (width != map2.GetLength(0) ||
            height != map2.GetLength(1))
            return null;

        float[,] newNoise = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = 0f;
                switch (opperation)
                {
                    case 0:
                        newNoise[y,x] = curve.Evaluate(map1[y,x] + map2[y,x]);
                        break;
                    case 1:
                        newNoise[y,x] = curve.Evaluate(map1[y,x] - map2[y,x]);
                        break;
                    case 2:
                        newNoise[y,x] = curve.Evaluate(map1[y,x] * map2[y,x]);
                        break;
                    case 3:
                        newNoise[y,x] = curve.Evaluate(map1[y,x] / map2[y,x]);
                        break;
                    case 4:
                        value = (map1[y,x] * map2[y,x] + Mathf.Abs(map1[y,x] * map2[y,x])) / 2f;
                        newNoise[y,x] = curve.Evaluate(value + (0.006f * map2[y,x]));
                        break;
                    case 5:
                        value = (map1[y,x] / map2[y,x] + Mathf.Abs(map1[y,x] * map2[y,x])) / 2f;
                        newNoise[y,x] = curve.Evaluate(value);
                        break;
                    default:
                        break;
                }
            }
        }

        return newNoise;
    }
    
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(new Vector2(transform.position.x, transform.position.z));
        
        MapDisplay display = GetComponent<MapDisplay>();
        
        
        if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.HumidityMap)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromHeightMap(mapData.heightMap, moistureCurve));
        else if (drawMode == DrawMode.DetailMap)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.detailMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromHeightMap(mapData.detailMap));
        else if (drawMode == DrawMode.ContinentMap)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.continentMap, 1, continentCurve, levelOfDetailEditor),
                TextureGen.TextureFromHeightMap(mapData.continentMap));
        else if (drawMode == DrawMode.HeightMap)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, levelOfDetailEditor),
                TextureGen.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.Continent)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.continentMap, 1, continentCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Details)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.detailMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Final)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        
        //vegeSystem.Vegetate(mapData.heightMap, heightMulti, transform.position, 10);
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
    public readonly float[,] detailMap;
    public readonly float[,] continentMap;
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] _detailMap, float[,] _continentMap, float[,] _heightMap, Color[] _colorMap)
    {
        detailMap = _detailMap;
        continentMap = _continentMap;
        heightMap = _heightMap;
        colorMap = _colorMap;
    }
}