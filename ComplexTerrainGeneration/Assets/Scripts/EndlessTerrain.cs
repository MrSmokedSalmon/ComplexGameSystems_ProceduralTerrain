using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float moveThresholdForCHUpdate = 25f;
    private const float sqrMoveThresholdForCHUpdate = moveThresholdForCHUpdate * moveThresholdForCHUpdate;
    
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    
    public Transform viewer;
    public Material mapMaterial;
    
    public static Vector2 viewerPos;
    private Vector2 viewerPosOld;
    private static HeightMapGen mapGen;
    
    private int chunkSize;
    private int chunksVisibleInViewDist;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    private void Start()
    {
        mapGen = GetComponent<HeightMapGen>();
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        chunkSize = HeightMapGen.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int i = 0; i < visibleChunks.Count; i++)
            visibleChunks[i].SetVisible(false);
        visibleChunks.Clear();
        
        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                        visibleChunks.Add(terrainChunkDictionary[viewedChunkCoord]);
                }
                else
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, mapMaterial));
            }
    }
    
    public class TerrainChunk
    {
        private GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;

        private MapData mapData;
        private bool mapDataRecieved;

        private int prevLODIndex = -1;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] _detailLevels, Material material)
        {
            detailLevels = _detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            
            meshObject.transform.position = positionV3;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            
            
            mapGen.RequestMapData(OnMapDataRecieved, position);
        }

        void OnMapDataRecieved(MapData _mapData)
        {
            mapData = _mapData;
            mapDataRecieved = true;

            meshRenderer.material.mainTexture = TextureGen.TextureFromColorMap(_mapData.colorMap, (int)bounds.size.x + 1, (int)bounds.size.y + 1);
            //meshRenderer.material.mainTexture = TextureGen.TextureFromHeightMap(mapData.continentMap);
        }

        public void UpdateChunk()
        {
            if (mapDataRecieved)
            {
                float viewerDistFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
                bool visible = viewerDistFromNearEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistFromNearEdge > detailLevels[i].visibleDistThreshold)
                            lodIndex = i + 1;
                        else
                            break;
                    }

                    if (lodIndex != prevLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            prevLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequested)
                            lodMesh.RequestMesh(mapData);
                    }
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequested;
        public bool hasMesh;
        private int lod;

        public LODMesh(int _lod)
        {
            lod = _lod;
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
        }
        
        public void RequestMesh(MapData mapData)
        {
            hasRequested = true;
            mapGen.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistThreshold;
    }
}


