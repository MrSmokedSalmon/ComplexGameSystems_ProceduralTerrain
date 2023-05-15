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
        NoiseMap,
        ColourMap,
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
    public AnimationCurve moistureCurve;

    public bool autoUpdate;
    public bool usePositionAsOffset;

    public TerrainTypes[] regions;
    public BiomeTypes[] biomes;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMapData(Action<MapData> callback, Vector2 position)
    {
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

    private MapData GenerateMapData(Vector2 _position)
    {
        float[,] heightMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, 
            octaveOffset, usePositionAsOffset ? _position : positionOffset, 
            octaves, persistance, lacunarity, seed);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        
        for (int y = 0; y < mapChunkSize; y++)
            for (int x = 0; x < mapChunkSize; x++)
            {
                //noiseMap[x, y] *= heightMask[x, y];
                float currentHeight = heightMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
            }

        return new MapData(heightMap, colorMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(new Vector2(transform.position.x, transform.position.z));
        
        MapDisplay display = GetComponent<MapDisplay>();
        
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(mapData.heightMap, heightCurve));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGen.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.HumidityMap)
            display.DrawTexture(TextureGen.TextureFromHeightMap(mapData.heightMap, moistureCurve));
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
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] _heightMap, Color[] _colorMap)
    {
        heightMap = _heightMap;
        colorMap = _colorMap;
    }
}