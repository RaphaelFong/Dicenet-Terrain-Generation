using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    public GameObject cubePrefab; // Each cubePrefab has a TileData attached
    public Material pre;
    public Material post;
    public Material recent;
    public TextMeshProUGUI targetDicenetText;   
    public TextMeshProUGUI warningText;

    public const int GRIDSIZE = 20;
    public const float TILESIZE = 1f; // my cubePrefab are slightly smaller than tileSize so can tell apart from each other

    public Dictionary<Vector3, GameObject> tileMap = new Dictionary<Vector3, GameObject>();
    public List<Dicenet> diceNets = new List<Dicenet>(); // Stores the 11 polyhedral dicenets
    public List<Vector3> tempHolder; // remember to clear after using

    public List<GameObject> mostRecentlyPlaced;

    public List<int> bag; // a bag is just a list of ints ig? eg 0010123
    public int bagIndex = 0; // index to traverse thru the bag
    public int targetDicenet = 0; // default set to 0th element in the diceNet list

    // Start is called before the first frame update
    void Start()
    {
        LoadDicenets();
        //VisualizeDicenet(10); // uncomment this and comment build grid to debug dicenet
        BuildGrid();
        //PlaceDicenet(2);
        //PlaceDicenet(2);
        //PlaceDicenet(2);
        //PlaceDicenet(2);
        //PlaceDicenet(1);
        //PlaceDicenet(0);

        foreach (int i in bag)
        {
            PlaceDicenet(i);
        }

        targetDicenetText.text = "Selected Dicenet Index : " + targetDicenet;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && targetDicenet != diceNets.Count - 1) 
        {
            targetDicenet++;
            //targetDicenet %= diceNets.Capacity;
            targetDicenetText.text = "Selected Dicenet Index : " + targetDicenet;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && targetDicenet != 0)
        {
            targetDicenet--;
            //targetDicenet %= diceNets.Capacity;
            targetDicenetText.text = "Selected Dicenet Index : " + targetDicenet;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //bagIndex++;
            //bagIndex %= bag.Capacity;
            PlaceDicenet(targetDicenet);
        }
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

    void PlaceDicenet(int diceNetIndex)
    {
        if (diceNetIndex > diceNets.Capacity - 1)
        {
            Debug.LogWarning("diceNetIndex out of bounds!"); ;
        }

        bool didZShift = true;

        // Placing First Dicenet
        if (IsGridEmpty())
        {
            Vector3 targetPos = new Vector3(0, 0, 0);

            bool placedSuccessful = false;

            while (!placedSuccessful)
            {
                tempHolder.Clear();

                foreach (Vector3 v3 in diceNets[diceNetIndex].offsets)
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
                        // This part of the algo shd consult chaw cos the placement gonna look weird
                        // He ok then ok ?
                        if (targetPos.z < GRIDSIZE && didZShift)
                        {
                            //Debug.Log("Attempting to +1 z");
                            targetPos.z += 1; // shift up 
                            didZShift = !didZShift;
                            break;
                        }
                        else if (targetPos.x < GRIDSIZE)
                        {
                            //Debug.Log("Attempting to +1 x");
                            targetPos.x += 1; // shift right
                            didZShift = !didZShift;
                            break;
                        }
                        else
                        {
                            // if reach here means no space on tile to place the dicenet
                            placedSuccessful = true; // sounds weird but we jus wanna break outta loop
                            warningText.text = "Insufficient space on board for dicenet index " + diceNetIndex;
                            Debug.LogError("Insufficient space on board!");
                        }

                    }

                    if (v3 == diceNets[diceNetIndex].offsets[diceNets[diceNetIndex].offsets.Length - 1])
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
            Debug.Log("Complete Placing First Dicenet");
        }
        
        // This one need calculate adjacency score
        else
        {
            #region GPT - with no consideration to rotation yet

            int bestScore = -1;
            Vector3 bestPlacement = Vector3.zero;
            List<Vector3> bestTilePositions = new();

            for (int x = 0; x < GRIDSIZE; x++)
            {
                for (int z = 0; z < GRIDSIZE; z++)
                {
                    Vector3 basePos = new Vector3(x, 0, z);
                    List<Vector3> potentialTiles = new();
                    bool canPlace = true;
                    int score = 0;

                    foreach (Vector3 offset in diceNets[diceNetIndex].offsets)
                    {
                        Vector3 newPos = basePos + offset;

                        if (!tileMap.ContainsKey(newPos)) { canPlace = false; break; }

                        var tile = tileMap[newPos].GetComponent<TileData>();
                        if (tile.isValid) { canPlace = false; break; }

                        potentialTiles.Add(newPos);

                        // Score this tile's 4 neighbors
                        foreach (Vector3 dir in new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                        {
                            Vector3 neighbor = newPos + dir;
                            if (tileMap.ContainsKey(neighbor))
                            {
                                if (tileMap[neighbor].GetComponent<TileData>().isValid)
                                {
                                    score++;
                                }
                            }
                        }
                    }

                    if (canPlace && score > bestScore)
                    {
                        bestScore = score;
                        bestPlacement = basePos;
                        bestTilePositions = new List<Vector3>(potentialTiles);
                    }
                }
            }

            // Clean up mostRecentlyPlaced

            foreach (GameObject go in mostRecentlyPlaced)
            {
                go.GetComponent<Renderer>().material = post;
            }
            mostRecentlyPlaced.Clear();

            if (bestTilePositions.Count > 0)
            {
                foreach (Vector3 pos in bestTilePositions)
                {
                    tileMap[pos].GetComponent<TileData>().isValid = true;
                    //tileMap[pos].GetComponent<Renderer>().material = post;
                    tileMap[pos].GetComponent<Renderer>().material = recent;

                    mostRecentlyPlaced.Add(tileMap[pos]);
                }
                //StartCoroutine(PlaceTilesWithDelay(bestTilePositions));

                Debug.Log($"Placed DiceNet at {bestPlacement} with adjacency score: {bestScore}");
            }
            else
            {
                warningText.text = "Insufficient space on board for dicenet index " + diceNetIndex;
                Debug.LogError("No valid placement found!");
            }


            #endregion
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

    //IEnumerator PlaceTilesWithDelay(List<Vector3> tilePos)
    //{
    //    foreach(Vector3 pos in tilePos)
    //    {
    //        tileMap[pos].GetComponent<TileData>().isValid = true;
    //        tileMap[pos].GetComponent<Renderer>().material = post;
    //        yield return new WaitForSeconds(1f);
    //    }
    //}

    void LoadDicenets()
    {
        // index 0
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

        // index 1
        /*
        0 X X 
        X X 0
        0 X 0
        0 X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -2),
            new Vector3(0, 0, -3),
        }));

        // index 2
        /*
        0 X X 
        0 X 0
        X X 0
        0 X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -2),
            new Vector3(0, 0, -2),
            new Vector3(0, 0, -3),
        }));

        // index 3
        /*
        0 X X 
        0 X 0
        0 X 0
        X X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -2),
            new Vector3(-1, 0, -3),
            new Vector3(0, 0, -3),
        }));

        // index 4
        /*
        0 X X 
        0 X 0
        0 X 0
        X X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, -1),
            new Vector3(-1, 0, -2),
            new Vector3(0, 0, -2),
            new Vector3(0, 0, -3),
        }));

        // index 5
        /*
        0 X 0 
        X X X
        0 X 0
        0 X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, -1),
            new Vector3(0, 0, -2),
            new Vector3(0, 0, -3),
        }));


        // index 6
        /*
        0 0 X
        X X X
        0 X 0
        0 X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(-2, 0, -1),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -2),
            new Vector3(-1, 0, -3),
        }));


        // index 7
        /*
        0 0 X
        0 X X
        X X 0
        0 X 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(-2, 0, -2),
            new Vector3(-1, 0, -2),
            new Vector3(-1, 0, -3),
        }));

        // index 8
        /*
        0 X X
        0 X 0
        X X 0
        X 0 0
         */

        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -2),
            new Vector3(0, 0, -2),
            new Vector3(-1, 0, -3),
        }));



        // index 9
        /*
        0 0 X 
        0 X X
        X X 0
        X 0 0
         */
        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(-2, 0, -2),
            new Vector3(-1, 0, -2),
            new Vector3(-2, 0, -3),
        }));

        // index 10
        /*
        0 X
        0 X
        X X
        X 0
        X 0
         */
        diceNets.Add(new Dicenet(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -2),
            new Vector3(0, 0, -2),
            new Vector3(-1, 0, -3),
            new Vector3(-1, 0, -4),
        }));
    }

    // Debugging the shape of dicenet
    // index is index of a specific shape stored in diceNets
    void VisualizeDicenet(int index)
    {
        foreach(Vector3 v3 in diceNets[index].offsets)
        {
            GameObject tile = Instantiate(cubePrefab, v3, Quaternion.identity, transform);
        }   
    }


}
