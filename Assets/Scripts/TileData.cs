using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileData : MonoBehaviour
{
    public bool isValid = false; // True means there's tile here but yet to assign biome
    public BiomeType biomeType = BiomeType.UNASSIGNED;
    public HashSet<BiomeType> possibleTypes = new HashSet<BiomeType>();
    

    // Start is called before the first frame update
    void Start()
    {
        // At the start it is technically possible for this tile
        // to be any of the possible BiomeType
        possibleTypes.Add(BiomeType.WATER);
        possibleTypes.Add(BiomeType.BEACH);
        possibleTypes.Add(BiomeType.PLAINS);
        possibleTypes.Add(BiomeType.FOREST);
        possibleTypes.Add(BiomeType.MOUNTAIN);
        possibleTypes.Add(BiomeType.SNOW);
        possibleTypes.Add(BiomeType.DESERT);
        possibleTypes.Add(BiomeType.SWAMP);
        possibleTypes.Add(BiomeType.VOLCANIC);
    }

    public void AssignRandomBiome()
    {
        int count = Enum.GetValues(typeof(BiomeType)).Length;
        Debug.Log(count);
        biomeType = (BiomeType)Random.Range(1, count);

        Debug.LogError("here");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
