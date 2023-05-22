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
    public AnimationCurve detailCurve;
    public AnimationCurve continentCurve;
    public AnimationCurve moistureCurve;

    public bool autoUpdate;
    public bool usePositionAsOffset;

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
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, levelOfDetailEditor);
        if (dMode == DrawMode.Continent)
            meshData = MeshGen.GenerateTerrainMesh(mapData.continentMap, 10, continentCurve, levelOfDetailEditor);
        else if (dMode == DrawMode.Details)
            meshData = MeshGen.GenerateTerrainMesh(mapData.detailMap, heightMulti, heightCurve, levelOfDetailEditor);
        
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
            octaves, persistance, lacunarity, seed, detailCurve);
        float[,] continentMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale * 3.0f, 
            octaveOffset, usePositionAsOffset ? _position : positionOffset, 
            6, persistance, lacunarity, seed, continentCurve);
        float[,] finalMap = CombineNoiseMaps(heightMap, continentMap, 4);
        
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        
        for (int y = 0; y < mapChunkSize; y++)
            for (int x = 0; x < mapChunkSize; x++)
            {
                //noiseMap[x, y] *= heightMask[x, y];
                float currentHeight = finalMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
            }

        return new MapData(heightMap, continentMap, finalMap, colorMap);
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
                        newNoise[x, y] = map1[x, y] + map2[x, y];
                        break;
                    case 1:
                        newNoise[x, y] = map1[x, y] - map2[x, y];
                        break;
                    case 2:
                        newNoise[x, y] = map1[x, y] * map2[x, y];
                        break;
                    case 3:
                        newNoise[x, y] = map1[x, y] / map2[x, y];
                        break;
                    case 4:
                        value = (map1[x, y] * map2[x, y] + Mathf.Abs(map1[x, y] * map2[x, y])) / 2f;
                        newNoise[x, y] = value + (0.006f * map2[x,y]);
                        break;
                    case 5:
                        value = (map1[x, y] / map2[x, y] + Mathf.Abs(map1[x, y] * map2[x, y])) / 2f;
                        newNoise[x, y] = value;
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
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.Continent)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.continentMap, 1, continentCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Details)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.detailMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Final)
            display.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMulti, heightCurve, levelOfDetailEditor),
                TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
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