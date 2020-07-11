using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScript : MonoBehaviour
{

    private const float speed = 30f;

    private const float tileSize = 100f;
    private const int tilesPerStage = 5;
    private const int numStages = 3;



    private GameObject playerPrefab;
    private List<List<GameObject>> groundTilePrefabs = new List<List<GameObject>>();
    private List<List<GameObject>> obstaclePrefabs = new List<List<GameObject>>();

    private GameObject player;
    private List<GameObject> groundTiles = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();

    private float distance = 0;


    
    void Awake()
    {
        for (int i = 0; i < numStages; i++)
        {
            groundTilePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("GroundTiles/"+i)));
            obstaclePrefabs.Add(new List<GameObject>(Resources.LoadAll<GameObject>("Obstacles/"+i)));
        }
        playerPrefab = Resources.Load<GameObject>("Player");
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame() {
        player = Instantiate(playerPrefab);
    }

    void Update()
    {
        float move = speed * Time.deltaTime;
        distance += move;

        player.transform.Translate(0, 0, move);
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
