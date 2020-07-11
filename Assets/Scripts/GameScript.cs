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



    private GameObject playerPrefab;
    private GameObject obstacleWrapperPrefab;
    private List<List<GameObject>> groundTilePrefabs = new List<List<GameObject>>();
    private List<List<GameObject>> obstaclePrefabs = new List<List<GameObject>>();

    private GameObject player;
    private List<GameObject> groundTiles = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();

    private float distance;

    private int nextTileCount;
    private float nextTileSpawnThreshold;


    
    void Awake()
    {
        for (int i = 0; i < numStages; i++)
        {
            groundTilePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("GroundTiles/"+i)));
            obstaclePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("Obstacles/"+i)));
        }
        playerPrefab = Resources.Load<GameObject>("Player");
        obstacleWrapperPrefab = Resources.Load<GameObject>("ObstacleWrapper");
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame() {
        distance = 0;
        nextTileCount = 0;
        nextTileSpawnThreshold = tileSize;

        player = Instantiate(playerPrefab);
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
    }

    public void EndGame() {
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
