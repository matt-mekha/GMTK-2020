using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneScript : MonoBehaviour
{

    private const float speed = 40f;

    private const int numTiles = 10;
    private const float tileSize = 100f;

    private int nextTile = 0;
    private float pos = 0;
    private float nextTilePos = tileSize;
    
    public GameObject player;

    private GameObject streetPrefab;
    private List<GameObject> streets = new List<GameObject>();

    void Awake()
    {
        streetPrefab = Resources.Load<GameObject>("GroundTiles/SceneStreet");
        for (int i = 0; i < numTiles; i++)
        {
            SpawnTile();
        }
    }

    void DespawnTile() {
        GameObject tile = streets[0];
        streets.RemoveAt(0);
        Destroy(tile);
    }

    void SpawnTile() {
        GameObject tile = Instantiate(streetPrefab, new Vector3(0, 0, nextTile * tileSize), Quaternion.identity);
        tile.transform.SetParent(transform);
        streets.Add(tile);
        nextTile++;
    }

    void Update()
    {
        float delta = speed * Time.deltaTime;
        pos += delta;
        player.transform.Translate(0, 0, delta);

        while(pos >= nextTilePos) {
            DespawnTile();
            SpawnTile();
            nextTilePos += tileSize;
        }
    }
}
