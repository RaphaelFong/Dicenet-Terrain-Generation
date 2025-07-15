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
    public List<BiomeType> possibleTypes = new List<BiomeType>();

    void Awake()
    {
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

    // Truly can be any biome
    public void AssignRandomBiome()
    {
        int count = Enum.GetValues(typeof(BiomeType)).Length;
        Debug.Log(count);
        biomeType = (BiomeType)Random.Range(1, count);

        Debug.LogError("here");
    }

    // A random biome from the hashmap of random
    public void AssignRandomBiomeFromListOfPossible()
    {
        int random = Random.Range(0, possibleTypes.Count - 1);
        biomeType = possibleTypes[random];
    }

    // Return number of element in possibleTypes
    public int Entropy()
    {
        return possibleTypes.Count;
    }

    public bool isCollapsed()
    {
        return biomeType != BiomeType.UNASSIGNED;
    }
}
