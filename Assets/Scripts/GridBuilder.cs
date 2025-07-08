using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    public GameObject cubePrefab; // Each cubePrefab has a TileData attached
    public Material pre;
    public Material post;
    public const int GRIDSIZE = 20;
    public const float TILESIZE = 1f;

    public Dictionary<Vector3, GameObject> tileMap = new Dictionary<Vector3, GameObject>();
    public List<Dicenet> diceNets = new List<Dicenet>(); // Stores the 11 polyhedral dicenets
    public List<Vector3> tempHolder; // remember to clear after using

    // Start is called before the first frame update
    void Start()
    {
        LoadDicenets();
        //VisualizeDicenet(0); // uncomment this and comment build grid to debug dicenet
        BuildGrid();
        PlaceDicenet();
    }

    // Builds from bot left (0,0,0) to top right (19,0,19)
    // Dun ask me why not start from top left, idw calc offset
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

    void PlaceDicenet()
    {
        // Placing First Dicenet
        if (IsGridEmpty())
        {
            Vector3 targetPos = new Vector3(0, 0, 0);

            bool placedSuccessful = false;

            while (!placedSuccessful)
            {
                tempHolder.Clear();

                Debug.Log("diceNets[0].offsets.Length : " + diceNets[0].offsets.Length);

                foreach (Vector3 v3 in diceNets[0].offsets)
                {
                    Vector3 newPos = targetPos + v3;

                    if (tileMap.ContainsKey(newPos))
                    {
                        if (tileMap[newPos].GetComponent<TileData>().isValid == false)
                        {
                            tempHolder.Add(newPos);
                        }
                    }
                    else
                    {
                        if (targetPos.z < TILESIZE)
                        {
                            Debug.Log("Attempting to +1 z");
                            targetPos.z += 1; // shift up 
                            break;
                        }
                        else if (targetPos.x < TILESIZE)
                        {
                            Debug.Log("Attempting to +1 x");
                            targetPos.x += 1; // shift right
                            break;
                        }
                        else
                        {
                            // if reach here means no space on tile to place the dicenet
                            placedSuccessful = true; // sounds weird but we jus wanna break outta loop
                            Debug.LogError("Insufficient space on tile!!!");
                        }

                    }

                    if (v3 == diceNets[0].offsets[diceNets[0].offsets.Length - 1])
                    {
                        placedSuccessful = true;
                    }
                }
            }

            foreach (Vector3 v3 in tempHolder)
            {
                tileMap[v3].GetComponent<TileData>().isValid = true;
                tileMap[v3].GetComponent<Renderer>().material = post;
            }
        }
    }

    // Return true if the 20x20 Grid is empty
    bool IsGridEmpty()
    {
        foreach (KeyValuePair<Vector3, GameObject> pair in tileMap)
        {
            TileData data = pair.Value.GetComponent<TileData>();
            if (data != null &&
                data.isValid)
            {
                return false;
            }
        }

        return true;
    }

    void LoadDicenets()
    {

        /*
        X X X 
          X
          X
          X
         */
        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0),
            new Vector3(1, 0, -1),
            new Vector3(1, 0, -2),
            new Vector3(1, 0, -3),
        }));
    }

    // Debugging the shape of dicenet
    // index is index of a specific shape stored in diceNets
    void VisualizeDicenet(int index)
    {
        foreach(Vector3 v3 in diceNets[index].offsets)
        {
            //Vector3 pos = new Vector3(x * TILESIZE, 0, z * TILESIZE);

            GameObject tile = Instantiate(cubePrefab, v3, Quaternion.identity, transform);
        }   
    }

    void Update()
    {
        
    }
}
