using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    public GameObject cubePrefab;
    public const int GRIDSIZE = 20;
    public const float TILESIZE = 1f;

    public Dictionary<Vector3, GameObject> tileMap = new Dictionary<Vector3, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        BuildGrid();
    }

    void BuildGrid()
    {
        for (int x = 0; x < GRIDSIZE; x++)
        {
            for (int z = 0; z < GRIDSIZE; z++)
            {
                Vector3 pos = new Vector3(x * TILESIZE, 0, z * TILESIZE);
                GameObject tile = Instantiate(cubePrefab, pos, Quaternion.identity, transform);
                tile.name = $"Tile ({x},{z})";
                tileMap[pos] = tile;
            }
        }

        Debug.Log($"Grid created with {tileMap.Count} tiles.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
