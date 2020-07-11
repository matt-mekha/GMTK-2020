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
    private const int startStage = 0; // for testing, should be 0
    private const int numEmptyTiles = 1;

    private const int obstaclesPerTileMin = 3;
    private const int obstaclesPerTileMax = 6;
    private const float obstacleXRange = 7f;

    public const int obstacleLayer = 8;

    private const float mouseDragFactor = 0.05f;

    private const float explosionForce = 4;
    private const float explosionRadius = 1;
    private const float explosionUpwardsFactor = -1;



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

    private bool selected;
    private GameObject hoveredObject;
    private List<GameObject> outlines = new List<GameObject>();

    private Vector3 lastMousePosition = new Vector3(0, 0, 0);

    public static bool alive = false;


    
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
        selected = false;
        alive = true;

        player = Instantiate(playerPrefab);
        player.GetComponentInChildren<PlayerScript>().gameScript = this;
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
        
        AddObstacleComponents(obstacle);
    }

    private void AddObstacleComponents(GameObject obj) {
        MeshCollider collider = obj.AddComponent<MeshCollider>();
        collider.convex = true;

        obj.layer = obstacleLayer;
        foreach(Transform child in obj.transform) {
            AddObstacleComponents(child.gameObject);
        }
    }

    private void SpawnNextTile() {
        int stage = (nextTileCount / tilesPerStage + startStage) % numStages;

        List<GameObject> tilePool = groundTilePrefabs[stage];
        GameObject tilePrefab = tilePool[Random.Range(0, tilePool.Count)];
        GameObject tile = Instantiate(tilePrefab, new Vector3(0, 0, nextTileCount * tileSize), Quaternion.identity);
        tile.AddComponent<MeshCollider>();
        groundTiles.Add(tile);

        if(nextTileCount >= numEmptyTiles) {
            int numObstacles = Random.Range(obstaclesPerTileMin, obstaclesPerTileMax);
            for (int i = 0; i < numObstacles; i++)
            {
                SpawnObstacle(tile, stage);
            }
        }

        nextTileCount++;
    }

    private void DespawnLastTile() {
        GameObject tile = groundTiles[0];
        groundTiles.RemoveAt(0);
        Destroy(tile);
    }

    void Update()
    {
        if(!alive) return;
        
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

        if(selected) {
            if(!Input.GetMouseButton(0)) {
                selected = false;
            } else {
                float mouseDeltaX = (Input.mousePosition - lastMousePosition).x;
                hoveredObject.transform.Translate(mouseDeltaX * mouseDragFactor, 0, 0);
            }
        } else {
            if(Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << obstacleLayer)) {
                hit.transform.SendMessageUpwards("OnHover", this);

                if(Input.GetMouseButton(0)) {
                    selected = true;
                }
            } else {
                OnHover(null);
            }
        }

        lastMousePosition = Input.mousePosition;
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

    public void OnCollision(Collision collision) {
        if((!alive) || collision.gameObject.layer != obstacleLayer) return;

        alive = false;
        OnHover(null);

        AddExplosionForce(player.GetComponentInChildren<Rigidbody>(), collision);
        AddExplosionForce(collision.GetContact(0).otherCollider.gameObject.AddComponent<Rigidbody>(), collision);
    }

    private void AddExplosionForce(Rigidbody rb, Collision collision) {
        rb.useGravity = true;
        rb.AddExplosionForce(explosionForce, collision.GetContact(0).point, explosionRadius, explosionUpwardsFactor, ForceMode.Impulse);
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
