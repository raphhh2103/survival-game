using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDst;
    public Transform viewer;
    public Material mapMaterial;


    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static WorldGenerator worldGenerator;
    int chunkSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionnary = new Dictionary<Vector2, TerrainChunk>();

    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        worldGenerator = FindObjectOfType<WorldGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstHold;
        chunkSize = WorldGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleInViewDst; yOffset < chunkVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDst; xOffset < chunkVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunckCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionnary.ContainsKey(viewedChunckCoord))
                {
                    terrainChunkDictionnary[viewedChunckCoord].UpdateTerrainChunk();
                    //if (terrainChunkDictionnary[viewedChunckCoord].IsVisible())
                    //{
                    //    terrainChunksVisibleLastUpdate.Add(terrainChunkDictionnary[viewedChunckCoord]);
                    //}
                }
                else
                {
                    terrainChunkDictionnary.Add(viewedChunckCoord, new TerrainChunk(viewedChunckCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailsLevel;
        LODMesh[] lODMeshes;

        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailsLevel, Transform parent, Material material)
        {
            this.detailsLevel = detailsLevel;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lODMeshes = new LODMesh[detailsLevel.Length];
            for (int i = 0; i < detailsLevel.Length; i++)
            {
                lODMeshes[i] = new LODMesh(detailsLevel[i].lod,UpdateTerrainChunk);
            }

            worldGenerator.RequestMapData(position ,OnMapDataReceived);
        }
        void OnMapDataReceived(MapData mapData)
        {

            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, WorldGenerator.mapChunkSize, WorldGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            // worldGenerator.RequestMeshData(mapData, OnMeshDataReveived);
        }

        //void OnMeshDataReveived(MeshData meshData)
        //{
        //    meshFilter.mesh = meshData.CreateMesh();
        //}

        public void UpdateTerrainChunk()
        {

            if (mapDataReceived)
            {

                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailsLevel.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailsLevel[i].visibleDstHold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lODMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
             
                terrainChunksVisibleLastUpdate.Add(this);
                
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
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;

        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            worldGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstHold;

    }

}
