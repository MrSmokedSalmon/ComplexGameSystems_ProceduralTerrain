using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float moveThresholdForCHUpdate = 25f;
    private const float sqrMoveThresholdForCHUpdate = moveThresholdForCHUpdate * moveThresholdForCHUpdate;
    
    public LODInfo[] detailLevels;

    public static int chunksLoadedData;
    public static int chunksLoadedMesh;
    public int chunksToLoad;

    [SerializeField] private float dataTime;
    [SerializeField] private float meshTime;
    
    [Min(0f)] public float plantViewDist;
    [Min(1f)] public float viewDist;
    public static float plantViewDistStatic;
    public static float viewDistStatic;
    public static float maxViewDist;
    
    public Transform viewer;
    public Material mapMaterial;

    public float maxTimeUnseen = 10f;
    
    public static Vector2 viewerPos;
    private Vector2 viewerPosOld;
    private static HeightMapGen mapGen;
    
    private int chunkSize;
    private int chunksVisibleInViewDist;

    public Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    private void Start()
    {
        mapGen = GetComponent<HeightMapGen>();
        
        // Sets the view distance, lower values equals less chunks loaded at a time
        viewDistStatic = viewDist;
        
        // Sets the minimum distance for chunks to spawn plants
        // If 0, plants are visible at the same range as chunks (NOT RECOMMENDED)
        plantViewDistStatic = plantViewDist == 0 ? viewDist : plantViewDist;
        plantViewDist = plantViewDistStatic;
        
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        viewDistStatic = viewDistStatic > maxViewDist ? maxViewDist : viewDistStatic;

        chunkSize = HeightMapGen.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    private void Update()
    {
        if (chunksLoadedData < chunksToLoad)
            dataTime += Time.deltaTime;
        if (chunksLoadedMesh < chunksToLoad)
            meshTime += Time.deltaTime;
        
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
                    if (terrainChunkDictionary[viewedChunkCoord].timeSinceLastSeen > maxTimeUnseen)
                    {
                        if (terrainChunkDictionary[viewedChunkCoord].SelfDelete())
                            terrainChunkDictionary.Remove(viewedChunkCoord);
                        if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                            Debug.Log("Delete didn't work");
                        else
                            continue;
                    }
                    
                    terrainChunkDictionary[viewedChunkCoord].UpdateChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                        visibleChunks.Add(terrainChunkDictionary[viewedChunkCoord]);
                }
                else
                {
                    Bounds cBounds = new Bounds(viewedChunkCoord * chunkSize, Vector2.one * chunkSize);
                    //TerrainChunk newChunk = new TerrainChunk(cBounds, detailLevels, mapMaterial);
                    if (Mathf.Sqrt(cBounds.SqrDistance(viewerPos)) <= viewDistStatic)
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(cBounds, detailLevels, mapMaterial));
                }
            }
    }
    
    public class TerrainChunk
    {
        private GameObject meshObject;
        private Vector2 position;
        public Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;

        private List<GameObject> vegetation;

        private MapData mapData;
        private bool mapDataRecieved;
        private bool plantsSpawned;

        public float timeSinceLastSeen;
        
        private int prevLODIndex = -1;
        
        public TerrainChunk(Bounds bound, LODInfo[] _detailLevels, Material material)
        {
            detailLevels = _detailLevels;
            bounds = bound;
            position = bound.center;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            CreateChunkGameObject(material, positionV3);
            
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);

            mapGen.RequestMapData(OnMapDataRecieved, position);
        }

        private void CreateChunkGameObject(Material material, Vector3 position)
        {
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            
            meshObject.transform.position = position;
        }

        public bool SelfDelete()
        {
            Destroy(meshObject);
            return meshObject;
        }

        void OnMapDataRecieved(MapData _mapData)
        {
            mapData = _mapData;
            mapDataRecieved = true;

            chunksLoadedData++;
            
            meshRenderer.material.mainTexture = TextureGen.TextureFromColorMap(_mapData.colorMap, (int)bounds.size.x + 1, (int)bounds.size.y + 1);
            //meshRenderer.material.mainTexture = TextureGen.TextureFromHeightMap(mapData.continentMap);
        }

        public void UpdateChunk()
        {
            if (mapDataRecieved)
            {
                float viewerDistFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
                bool visible = viewerDistFromNearEdge <= viewDistStatic;
                
                if (visible)
                {
                    timeSinceLastSeen = 0f;
                    
                    if (viewerDistFromNearEdge < plantViewDistStatic)
                    {
                        if (!plantsSpawned)
                        {
                            plantsSpawned = true;
                            SpawnPlants();
                            Debug.Log("Spawn plants");
                        }
                    }
                    else if (plantsSpawned)
                    {
                        plantsSpawned = false;
                        DestroyPlants();
                        Debug.Log("Destroy plants");
                    }

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
                else
                {
                    if (plantsSpawned)
                    {
                        plantsSpawned = false;
                        DestroyPlants();
                        Debug.Log("Destroy plants");
                    }

                    timeSinceLastSeen += Time.deltaTime;
                }
                

                SetVisible(visible);
            }
        }

        private void DestroyPlants()
        {
            foreach (GameObject plant in vegetation)
            {
                Destroy(plant);
            }
            vegetation.Clear();
        }

        private void SpawnPlants()
        {
            vegetation = VegetationSystem.SpawnPlants(mapData.vegetation);
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
            chunksLoadedMesh++;
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


