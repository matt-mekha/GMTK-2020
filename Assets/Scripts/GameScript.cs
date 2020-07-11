using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScript : MonoBehaviour
{

    private const float speed = 30f;

    private const float tileSize = 100f;
    private const int tilesPerStage = 5;
    private const int numTilesAtOnce = 7;
    private const int numStages = 3;
    private const int startStage = 0; // TODO should be 0

    private const int obstaclesPerTileMin = 3;
    private const int obstaclesPerTileMax = 6;
    private const float obstacleXRange = 7f;

    private const int obstacleLayer = 8;



    private GameObject playerPrefab;
    private GameObject obstacleWrapperPrefab;
    private GameObject outlinePrefab;
    private List<List<GameObject>> groundTilePrefabs = new List<List<GameObject>>();
    private List<List<GameObject>> obstaclePrefabs = new List<List<GameObject>>();

    private GameObject player;
    private new Camera camera;
    private List<GameObject> groundTiles = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();

    private float distance;

    private int nextTileCount;
    private float nextTileSpawnThreshold;

    private GameObject hoveredObject;
    private List<GameObject> outlines = new List<GameObject>();


    
    void Awake()
    {
        for (int i = 0; i < numStages; i++)
        {
            groundTilePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("GroundTiles/"+i)));
            obstaclePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("Obstacles/"+i)));
        }
        playerPrefab = Resources.Load<GameObject>("Player");
        obstacleWrapperPrefab = Resources.Load<GameObject>("ObstacleWrapper");
        outlinePrefab = Resources.Load<GameObject>("Outline");
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame() {
        distance = 0;
        nextTileCount = 0;
        nextTileSpawnThreshold = tileSize;
        hoveredObject = null;

        player = Instantiate(playerPrefab);
        camera = player.transform.Find("Camera").GetComponent<Camera>();
        for (int i = 0; i < numTilesAtOnce; i++)
        {
            SpawnNextTile();
        }
    }

    private void SpawnObstacle(GameObject tile, int stage) {
        List<GameObject> obstaclePool = obstaclePrefabs[stage];
        GameObject obstaclePrefab = obstaclePool[Random.Range(0, obstaclePool.Count)];
        GameObject obstacleWrapper = Instantiate(obstacleWrapperPrefab, tile.transform);
        GameObject obstacle = Instantiate(obstaclePrefab, obstacleWrapper.transform);
        obstacleWrapper.transform.localPosition = new Vector3(Random.Range(-obstacleXRange, obstacleXRange), 0, Random.Range(-tileSize/2, tileSize/2)) / 10f;
        
        AddMeshCollider(obstacle);
    }

    private void AddMeshCollider(GameObject obj) {
        obj.AddComponent<MeshCollider>();
        obj.layer = obstacleLayer;
        foreach(Transform child in obj.transform) {
            AddMeshCollider(child.gameObject);
        }
    }

    private void SpawnNextTile() {
        int stage = (nextTileCount / tilesPerStage + startStage) % numStages;

        List<GameObject> tilePool = groundTilePrefabs[stage];
        GameObject tilePrefab = tilePool[Random.Range(0, tilePool.Count)];
        GameObject tile = Instantiate(tilePrefab, new Vector3(0, 0, nextTileCount * tileSize), Quaternion.identity);
        groundTiles.Add(tile);

        nextTileCount++;

        int numObstacles = Random.Range(obstaclesPerTileMin, obstaclesPerTileMax);
        for (int i = 0; i < numObstacles; i++)
        {
            SpawnObstacle(tile, stage);
        }
    }

    private void DespawnLastTile() {
        GameObject tile = groundTiles[0];
        groundTiles.RemoveAt(0);
        Destroy(tile);
    }

    void Update()
    {
        float move = speed * Time.deltaTime;
        distance += move;

        player.transform.Translate(0, 0, move);

        while (distance >= nextTileSpawnThreshold) {
            nextTileSpawnThreshold += tileSize;
            DespawnLastTile();
            SpawnNextTile();
        }


        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << obstacleLayer)) {
            hit.transform.SendMessageUpwards("OnHover", this);
        } else {
            OnHover(null);
        }
    }

    public void OnHover(GameObject newHoveredObject) {
        if(hoveredObject == newHoveredObject) return;

        hoveredObject = newHoveredObject;

        foreach(GameObject outline in outlines) {
            Destroy(outline);
        }
        outlines.Clear();

        if(hoveredObject != null) {
            Outline(hoveredObject.transform);
        }
    }

    private void Outline(Transform parent) {
        if(parent.tag == "Outline") return;

        MeshFilter meshFilter = parent.GetComponent<MeshFilter>();
        if(meshFilter != null) {
            GameObject outline = Instantiate(outlinePrefab, parent);
            outline.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
            outlines.Add(outline);
        }

        foreach (Transform child in parent)
        {
            Outline(child);
        }
    }

    public void EndGame() {
        OnHover(null);
        Destroy(player);
        foreach (GameObject groundTile in groundTiles)
        {
            Destroy(groundTile);
        }
        foreach (GameObject obstacle in obstacles)
        {
            Destroy(obstacle);
        }
    }

}
