using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class GameScript : MonoBehaviour
{

    private const float startSpeed = 30f;
    private const float acceleration = 0.4f;

    private const float tileSize = 100f;
    private const int tilesPerStage = 5;
    private const int numTilesAtOnce = 7;
    private const int numStages = 3;
    private const int startStage = 0; // for testing, should be 0
    private const int numEmptyTiles = 2;

    private const int obstaclesPerTileMin = 3;
    private const int obstaclesPerTileMax = 6;
    private const float obstacleXRange = 7f;

    public const int obstacleLayer = 8;

    private const float mouseDragFactor = 0.05f;

    private const float explosionForce = 4;
    private const float explosionRadius = 1;
    private const float explosionUpwardsFactor = -1;

    private const float expertModeObstacleFactor = 1.5f;

    private const float nextHonkMin = 2f;
    private const float nextHonkMax = 8f;


    private GameObject playerPrefab;
    private GameObject obstacleWrapperPrefab;
    private GameObject outlinePrefab;
    private GameObject mountainPrefab;
    private List<List<GameObject>> groundTilePrefabs = new List<List<GameObject>>();
    private List<List<GameObject>> obstaclePrefabs = new List<List<GameObject>>();
    private List<GameObject> transitionTilePrefabs = new List<GameObject>();

    private GameObject player;
    private new Camera camera;
    private List<GameObject> groundTiles = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();

    private float distance;
    private float speed;

    private int nextTileCount;
    private float nextTileSpawnThreshold;

    private bool selected;
    private GameObject hoveredObject;
    private List<GameObject> outlines = new List<GameObject>();

    private Vector3 lastMousePosition = new Vector3(0, 0, 0);

    public static bool alive = false;

    private int lastStageSpawned;

    public Text inGameScoreText;
    public GameObject inGameFolder;
    public GameObject gameOverFolder;
    public Text gameOverScoreText;
    public Text gameOverHighScoreText;

    public Transform soundFolder;

    private Collision collisionCache;

    private bool tutorialSuccess;
    public Transform tutorialMessages;

    public static bool expertMode = false;

    private float nextHonk;
    private float honkTime;

    private string path;

    private int highScoreNormal = 0;
    private int highScoreExpert = 0;


    
    void Awake()
    {
        for (int i = 0; i < numStages; i++)
        {
            groundTilePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("GroundTiles/"+i)));
            obstaclePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("Obstacles/"+i)));
            transitionTilePrefabs.Add(Resources.Load<GameObject>("GroundTiles/T"+i));
        }
        playerPrefab = Resources.Load<GameObject>("Player");
        mountainPrefab = Resources.Load<GameObject>("GroundTiles/Mountains");
        obstacleWrapperPrefab = Resources.Load<GameObject>("ObstacleWrapper");
        outlinePrefab = Resources.Load<GameObject>("Outline");

        path = Application.persistentDataPath + "/highScore.txt";
        Load();
    }
    
    void Save() {
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(highScoreNormal);
        writer.WriteLine(highScoreExpert);
        writer.Close();
    }

    void Load() {
        if(File.Exists(path)) {
            StreamReader reader = new StreamReader(path);
            highScoreNormal = System.Convert.ToInt32(reader.ReadLine());
            highScoreExpert = System.Convert.ToInt32(reader.ReadLine());
            reader.Close();
        }
    }

    private AudioSource GetSound(string name) {
        return soundFolder.Find(name).GetComponent<AudioSource>();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Pause() {
        Time.timeScale = 0;
    }

    public void Resume() {
        Time.timeScale = 1;
    }

    private void UpdateScore() {
        inGameScoreText.text = "" + Mathf.RoundToInt(distance);
    }

    public void SetExpertMode(bool expert) {
        expertMode = expert;
    }

    public void StartGame(bool tutorial) {
        Resume();

        distance = 0;
        nextTileCount = 0;
        nextTileSpawnThreshold = tileSize;
        hoveredObject = null;
        selected = false;
        alive = true;
        lastStageSpawned = startStage;
        speed = startSpeed;
        nextHonk = nextHonkMin;
        honkTime = 0;

        UpdateScore();

        player = Instantiate(playerPrefab);
        player.GetComponentInChildren<PlayerScript>().gameScript = this;
        camera = player.transform.Find("Camera").GetComponent<Camera>();
        for (int i = 0; i < numTilesAtOnce; i++)
        {
            GameObject tile = SpawnNextTile();
            if(tutorial && i == 1) {
                SpawnObstacle(tile, 0, true);
            }
        }

        if(expertMode) {
            player.GetComponentInChildren<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
        }

        if(tutorial) {
            StartCoroutine(Tutorial());
        }

        GetSound("Engine").Play();
    }

    private IEnumerator Tutorial() {
        ToggleTutorialMessage(0, true);
        ToggleTutorialMessage(1, false);
        tutorialSuccess = false;
        yield return new WaitForSeconds(2.5f);
        Time.timeScale = 0;
        while(!tutorialSuccess) {
            yield return new WaitForEndOfFrame();
        }
        Time.timeScale = 1;
        ToggleTutorialMessage(0, false);
        ToggleTutorialMessage(1, true);
        yield return new WaitForSeconds(5);
        ToggleTutorialMessage(1, false);
    }

    private void ToggleTutorialMessage(int num, bool on) {
        tutorialMessages.GetChild(num).gameObject.SetActive(on);
    }

    private void SpawnObstacle(GameObject tile, int stage, bool tutorial = false) {
        List<GameObject> obstaclePool = obstaclePrefabs[stage];
        GameObject obstaclePrefab = obstaclePool[Random.Range(0, obstaclePool.Count)];
        GameObject obstacleWrapper = Instantiate(obstacleWrapperPrefab, tile.transform);
        GameObject obstacle = Instantiate(obstaclePrefab, obstacleWrapper.transform);
        
        Vector3 pos = new Vector3(Random.Range(-obstacleXRange, obstacleXRange), 0, Random.Range(-tileSize/2, tileSize/2)) / 10f;
        if(tutorial) {
            pos = new Vector3(0, 0, 0);
        }
        obstacleWrapper.transform.localPosition = pos;
        
        AddObstacleComponents(obstacle);

        Rigidbody rb = obstacle.AddComponent<Rigidbody>();
        rb.mass = 3;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    private void AddObstacleComponents(GameObject obj) {
        MeshCollider collider = obj.AddComponent<MeshCollider>();
        collider.convex = true;

        obj.layer = obstacleLayer;
        foreach(Transform child in obj.transform) {
            AddObstacleComponents(child.gameObject);
        }
    }

    private GameObject SpawnNextTile() {
        int stage = (nextTileCount / tilesPerStage + startStage) % numStages;

        GameObject tilePrefab;
        bool transition = stage != lastStageSpawned;
        if(transition) {
            tilePrefab = transitionTilePrefabs[lastStageSpawned];
        } else {
            List<GameObject> tilePool = groundTilePrefabs[stage];
            tilePrefab = tilePool[Random.Range(0, tilePool.Count)];
        }

        
        GameObject tile = Instantiate(tilePrefab, new Vector3(0, 0, nextTileCount * tileSize), Quaternion.identity);
        if(expertMode) {
            AddColliders(tile.transform);
        } else {
            RemoveColliders(tile.transform);
        }

        tile.AddComponent<MeshCollider>();
        groundTiles.Add(tile);

        GameObject mountainTile = Instantiate(mountainPrefab, tile.transform);
        mountainTile.transform.localPosition = new Vector3(-10, -3, 0);
        RemoveColliders(mountainTile.transform);

        if(nextTileCount >= numEmptyTiles && !transition) {
            int numObstacles = Random.Range(obstaclesPerTileMin, obstaclesPerTileMax);
            if(expertMode) {
                numObstacles = (int) (numObstacles * expertModeObstacleFactor);
            }
            for (int i = 0; i < numObstacles; i++)
            {
                SpawnObstacle(tile, stage);
            }
        }

        nextTileCount++;
        lastStageSpawned = stage;

        return tile;
    }

    private void RemoveColliders(Transform parent) {
        MeshCollider mc = parent.GetComponent<MeshCollider>();
        if(mc != null) Destroy(mc);

        foreach(Transform child in parent) {
            RemoveColliders(child);
        }
    }

    private void AddColliders(Transform parent) {
        if(parent.GetComponent<MeshFilter>() != null && parent.GetComponent<MeshCollider>() == null) {
            parent.gameObject.AddComponent<MeshCollider>();
        }

        foreach(Transform child in parent) {
            AddColliders(child);
        }
    }

    private void DespawnLastTile() {
        GameObject tile = groundTiles[0];
        groundTiles.RemoveAt(0);
        Destroy(tile);
    }

    void Update()
    {
        if(!alive) return;

        float accel = acceleration * Time.deltaTime;
        speed += accel;
        if(expertMode) speed += accel;
        
        float move = speed * Time.deltaTime;
        distance += move;

        player.transform.Translate(0, 0, move);

        while (distance >= nextTileSpawnThreshold) {
            nextTileSpawnThreshold += tileSize;
            DespawnLastTile();
            SpawnNextTile();
        }

        UpdateScore();

        honkTime += Time.deltaTime;
        while (honkTime >= nextHonk) {
            GetSound("Honk").Play();
            nextHonk += Random.Range(nextHonkMin, nextHonkMax);
        }
    }

    void FixedUpdate() {
        if(!alive) return;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if(selected) {
            if(!Input.GetMouseButton(0)) {
                selected = false;
                tutorialSuccess = true;
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
            //Outline(hoveredObject.transform);
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
        Resume();

        collisionCache = collision;
        AddExplosionForce(player.GetComponentInChildren<Rigidbody>());
        collision.GetContact(0).otherCollider.gameObject.SendMessageUpwards("OnCollision", this);

        GetSound("Crash").Play();
        GetSound("Music").Pause();

        StartCoroutine(GameOver());
    }

    public void OnCollision(Rigidbody rb) {
        AddExplosionForce(rb);
    }

    private IEnumerator GameOver() {
        yield return new WaitForSeconds(2);

        int score = Mathf.RoundToInt(distance);
        
        inGameFolder.SetActive(false);
        gameOverFolder.SetActive(true);
        gameOverScoreText.text = "Score: " + score;

        if(expertMode) {
            if(score > highScoreExpert) {
                highScoreExpert = score;
                Save();
            }
            gameOverHighScoreText.text = "High score: " + highScoreExpert;
        } else {
            if(score > highScoreNormal) {
                highScoreNormal = score;
                Save();
            }
            gameOverHighScoreText.text = "High score: " + highScoreNormal;
        }

        GetSound("GameOver").Play();
    }

    private void AddExplosionForce(Rigidbody rb) {
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.AddExplosionForce(explosionForce, collisionCache.GetContact(0).point, explosionRadius, explosionUpwardsFactor, ForceMode.Impulse);
    }

    public void CleanUp() {
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
