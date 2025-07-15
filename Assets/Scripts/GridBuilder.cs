//using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
//using static UnityEditor.Experimental.GraphView.GraphView;
//using UnityEngine.Assertions;

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using UnityEngine.XR;

public enum BiomeType
{
    UNASSIGNED, // ALL TILE BY DEFAULT SHOULD BE UNASSIGNED !
    WATER,
    BEACH,
    PLAINS,
    FOREST,
    MOUNTAIN,
    SNOW,
    DESERT,
    SWAMP,
    VOLCANIC
};

public class GridBuilder : MonoBehaviour
{
    public GameObject cubePrefab; // Each cubePrefab has a TileData attached
    public Material pre;
    public Material post;
    public Material recent;
    public TextMeshProUGUI targetDicenetText;
    public TextMeshProUGUI warningText;

    public Material waterMat;
    public Material beachMat;
    public Material plainsMat;
    public Material forestMat;
    public Material mountainMat;
    public Material snowMat;
    public Material desertMat;
    public Material swampMat;
    public Material volcanicMat;

    public const int GRIDSIZE = 20;
    public const float TILESIZE = 1f; // my cubePrefab are slightly smaller than tileSize so can tell apart from each other

    public Dictionary<Vector3, GameObject> tileMap = new Dictionary<Vector3, GameObject>();
    private List<Dicenet> diceNets = new List<Dicenet>(); // Stores the 11 polyhedral dicenets
    public List<Vector3> tempHolder; // remember to clear after using

    public List<GameObject> mostRecentlyPlaced; // just for some "highlighting" effects

    public List<int> bag; // a bag is just a list of ints ig? eg 0010123
    public int bagIndex = 0; // index to traverse thru the bag
    public int targetDicenet = 0; // default set to 0th element in the diceNet list

    // stores the allowed neighbours in 4 directions UP DOWN LEFT RIGHT
    public Dictionary<BiomeType, List<BiomeType>> heuristics = new Dictionary<BiomeType, List<BiomeType>>();

    // 4 Directions
    public Vector3[] directions = new Vector3[] { new Vector3(1,0,0),
                                                  new Vector3(-1,0,0),
                                                  new Vector3(0,0,1),
                                                  new Vector3(0,0,-1) };


    // Start is called before the first frame update
    void Start()
    {
        LoadDicenets();
        LoadHeuristics();

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

        //BuildBiomes(); // Shifted to Update
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && targetDicenet != diceNets.Count - 1)
        {
            targetDicenet++;
            targetDicenetText.text = "Selected Dicenet Index : " + targetDicenet;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && targetDicenet != 0)
        {
            targetDicenet--;
            targetDicenetText.text = "Selected Dicenet Index : " + targetDicenet;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlaceDicenet(targetDicenet);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {

            BuildBiomes();
        }
    }

    // Builds from bot left (0,0,0) to top right (19,0,19)
    // Dun ask me why not start from top left, idw calc offset
    void BuildGrid()
    {
        int index = 1;

        for (int x = 0; x < GRIDSIZE; x++)
        {
            for (int z = 0; z < GRIDSIZE; z++)
            {
                Vector3 pos = new Vector3(x * TILESIZE, 0, z * TILESIZE);
                GameObject tile = Instantiate(cubePrefab, pos, Quaternion.identity, transform);
                tile.name = index + $"Tile ({x},{z})";
                tileMap[pos] = tile;
                index++;
            }
        }
        //Debug.Log($"Grid created with {tileMap.Count} tiles.");
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
            //Debug.Log("Complete Placing First Dicenet");
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

                //Debug.Log($"Placed DiceNet at {bestPlacement} with adjacency score: {bestScore}");
            }
            else
            {
                warningText.text = "Insufficient space on board for dicenet index " + diceNetIndex;
                //Debug.LogError("No valid placement found!");
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

    void LoadHeuristics()
    {
        heuristics.Add(BiomeType.WATER, new List<BiomeType>
        {
            BiomeType.WATER,
            BiomeType.BEACH,
            BiomeType.PLAINS
        });

        heuristics.Add(BiomeType.BEACH, new List<BiomeType>
        {
            BiomeType.BEACH,
            BiomeType.WATER,
            BiomeType.PLAINS,
            BiomeType.FOREST,
            BiomeType.DESERT
        });


        heuristics.Add(BiomeType.PLAINS, new List<BiomeType>
        {
            BiomeType.WATER,
            BiomeType.BEACH,
            BiomeType.PLAINS,
            BiomeType.FOREST,
            BiomeType.MOUNTAIN,
            BiomeType.SNOW,
            BiomeType.DESERT,
            BiomeType.SWAMP,
            BiomeType.VOLCANIC
        });

        heuristics.Add(BiomeType.FOREST, new List<BiomeType>
        {
            BiomeType.FOREST,
            BiomeType.PLAINS,
            BiomeType.MOUNTAIN,
            BiomeType.SWAMP,
            BiomeType.BEACH
        });


        heuristics.Add(BiomeType.MOUNTAIN, new List<BiomeType>
        {
            BiomeType.MOUNTAIN,
            BiomeType.PLAINS,
            BiomeType.FOREST,
            BiomeType.SNOW
        });

        heuristics.Add(BiomeType.SNOW, new List<BiomeType>
        {
            BiomeType.SNOW,
            BiomeType.MOUNTAIN,
            BiomeType.PLAINS
        });

        heuristics.Add(BiomeType.DESERT, new List<BiomeType>
        {
            BiomeType.DESERT,
            BiomeType.PLAINS,
            BiomeType.BEACH,
            BiomeType.VOLCANIC
        });

        heuristics.Add(BiomeType.SWAMP, new List<BiomeType>
        {
            BiomeType.SWAMP,
            BiomeType.WATER,
            BiomeType.FOREST,
            BiomeType.PLAINS
        });

        heuristics.Add(BiomeType.VOLCANIC, new List<BiomeType>
        {
            BiomeType.VOLCANIC,
            BiomeType.MOUNTAIN,
            BiomeType.DESERT
        });
    }

    List<GameObject> propogationQueue = new List<GameObject>();

    // Propagate
    void BuildBiomes()
    {
        // Choose the first random thing, note this only selects the first valid in list
        //foreach (KeyValuePair<Vector3, GameObject> pair in tileMap)
        //{
        //    TileData data = pair.Value.GetComponent<TileData>();

        //    if (data.isValid == true)
        //    {
        //        data.AssignRandomBiome();
        //        //ChangeMaterial(pair.Value);
        //        propogationQueue.Add(pair.Value);
        //        break;
        //    }
        //}

        // 1. FULL RESET
        propogationQueue.Clear();

        foreach (var kv in tileMap)
        {
            TileData data = kv.Value.GetComponent<TileData>();
            if (data.isValid)
            {
                // Reset all valid tiles
                data.biomeType = BiomeType.UNASSIGNED;
                data.possibleTypes = new List<BiomeType>() {
                BiomeType.WATER, BiomeType.BEACH, BiomeType.PLAINS,
                BiomeType.FOREST, BiomeType.MOUNTAIN, BiomeType.SNOW,
                BiomeType.DESERT, BiomeType.SWAMP, BiomeType.VOLCANIC
            };
                kv.Value.GetComponent<Renderer>().material = post; // Reset visual
            }
        }

        // 2. NEW SEED SELECTION
        var validTiles = tileMap.Values
            .Where(go => go.GetComponent<TileData>().isValid)
            .ToList();

        if (validTiles.Count > 0)
        {
            // Random seed tile
            GameObject seedTile = validTiles[Random.Range(0, validTiles.Count)];
            seedTile.GetComponent<TileData>().AssignRandomBiomeFromListOfPossible();
            propogationQueue.Add(seedTile);
        }

        // 3. PROCESSING LOOP

        // Set a temp limit in case it goes infinite loop
        int recursionCount = 0;
        int recursionMax = 1000;

        // While there is something in the queue
        while (propogationQueue.Count > 0 && recursionCount < recursionMax)
        {
            // 1. COLLAPSE PHASE (separate this)
            GameObject toCollapse = GetLowestEntropyTile(propogationQueue);
            CollapseTile(toCollapse);
            propogationQueue.Remove(toCollapse);

            // 2. PROPAGATE PHASE (only from the just-collapsed tile)
            PropagateConstraints(toCollapse);

            recursionCount++;

            if (recursionCount >= recursionMax)
                Debug.Log("RECURSION MAX REACHED !");
        }
    }

    #region god

    GameObject GetLowestEntropyTile(List<GameObject> queue)
    {
        GameObject lowest = null;
        int minEntropy = int.MaxValue;

        foreach (GameObject go in queue)
        {
            int e = go.GetComponent<TileData>().Entropy();
            if (e < minEntropy)
            {
                minEntropy = e;
                lowest = go;
            }
        }
        return lowest;
    }

    void CollapseTile(GameObject tile)
    {
        TileData data = tile.GetComponent<TileData>();
        data.AssignRandomBiomeFromListOfPossible();
        ChangeMaterial(tile);
    }

    void PropagateConstraints(GameObject source)
    {
        TileData sourceData = source.GetComponent<TileData>();

        foreach (var dir in directions)
        {
            Vector3 neighborPos = source.transform.position + dir;

            if (tileMap.TryGetValue(neighborPos, out GameObject neighbor))
            {
                TileData neighborData = neighbor.GetComponent<TileData>();

                if (!neighborData.isValid || neighborData.isCollapsed())
                    continue;

                // Filter possibilities
                List<BiomeType> newPossible = neighborData.possibleTypes
                    .Where(b => heuristics[sourceData.biomeType].Contains(b))
                    .ToList();

                // Only if possibilities changed
                if (newPossible.Count != neighborData.possibleTypes.Count)
                {
                    neighborData.possibleTypes = newPossible;

                    if (!propogationQueue.Contains(neighbor))
                    {
                        propogationQueue.Add(neighbor);
                    }
                }
            }
        }
    }

    #endregion


    // WFC 
    // At this step we just put the possible ones first
    // origin is vec3 where this instance of propagation happens around
    void PropogateBiome(GameObject source)
    {
        TileData sourceData = source.GetComponent<TileData>();

        int lowest = int.MaxValue;
        GameObject lowestGO = null; // temp holder to pop the go with lowest entropy

        // Collapse lowest entropy
        foreach(GameObject go in propogationQueue)
        {
            if (lowest > go.GetComponent<TileData>().Entropy())
            {
                lowest = go.GetComponent<TileData>().Entropy();
                lowestGO = go;
            }
        }

        lowestGO.GetComponent<TileData>().AssignRandomBiomeFromListOfPossible();
        ChangeMaterial(lowestGO);
        propogationQueue.Remove(lowestGO);


        // Verify valid
        //if (!sourceData.isValid || sourceData.biomeType == BiomeType.UNASSIGNED)
        if (!sourceData.isValid)
            {
            Debug.LogWarning("Invalid PropogateBiome");
            return;
        }

        foreach (var dir in directions)
        {
            Vector3 neighbour = new Vector3(
                source.transform.position.x + dir.x,
                0,
                source.transform.position.z + dir.y);

            if (source.transform.position == neighbour)
                continue;

            if (tileMap.TryGetValue(neighbour, out GameObject neighbourGO))
            {
                // Check if can be adjacent
                TileData neighbourData = neighbourGO.GetComponent<TileData>();

                if (!neighbourData.isValid || neighbourData.isCollapsed())
                    continue;

                BiomeType sourceBiome = sourceData.biomeType;

                // This line causing error because heuristics dun contain UNDEFINED
                List<BiomeType> allowed = heuristics[sourceBiome];

                List<BiomeType> filtered = new List<BiomeType>();

                // looks thru the curr possibleTypes and adds them to the
                // filtered hashset
                // afterwards the possibleTypes will also be set to this filtered version

                foreach (BiomeType type in neighbourData.possibleTypes)
                {
                    if (allowed.Contains(type))
                        filtered.Add(type);
                }

                if (filtered.Count != neighbourData.possibleTypes.Count)
                {
                    neighbourData.possibleTypes = filtered;

                    // Handle contradiction (no possible types left)
                    //if (neighbourData.possibleTypes.Count == 0)
                    //{
                    //    neighbourData.isValid = false;
                    //    continue;
                    //}

                    // Collapse if only 1 possible biome 
                    // Else continue
                    //if (neighbourData.possibleTypes.Count == 1)
                    //{
                    //    neighbourData.AssignRandomBiomeFromListOfPossible();
                    //    ChangeMaterial(neighbourGO);
                    //}

                    if (!propogationQueue.Contains(neighbourGO))
                    {
                        propogationQueue.Add(neighbourGO);
                    }

                }
            }
        }
    }

    // Returns true if lhs and rhs can be adjacent 
    bool CanBiomesBeAdjacent(BiomeType lhs, BiomeType rhs)
    {
        return heuristics.ContainsKey(lhs) && heuristics[lhs].Contains(rhs);
    }

    void ChangeMaterial(GameObject go)
    {
        switch (go.GetComponent<TileData>().biomeType)
        { 
            case BiomeType.WATER: { go.GetComponent<Renderer>().material = waterMat; break; }
            case BiomeType.BEACH: { go.GetComponent<Renderer>().material = beachMat; break; }
            case BiomeType.PLAINS: { go.GetComponent<Renderer>().material = plainsMat; break; }
            case BiomeType.FOREST: { go.GetComponent<Renderer>().material = forestMat; break; }
            case BiomeType.MOUNTAIN: { go.GetComponent<Renderer>().material = mountainMat; break; }
            case BiomeType.SNOW: { go.GetComponent<Renderer>().material = snowMat; break; }
            case BiomeType.DESERT: { go.GetComponent<Renderer>().material = desertMat; break; }
            case BiomeType.SWAMP: { go.GetComponent<Renderer>().material = swampMat; break; }
            case BiomeType.VOLCANIC: { go.GetComponent<Renderer>().material = volcanicMat; break; }
        }
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
