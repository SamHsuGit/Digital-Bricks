using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "ProcGen/Biome")]
public class Biome : ScriptableObject
{
    [Header("Biome Attributes")]
    public string biomeName;
    public int offset;
    public float scale;
    public int terrainHeight;
    public float terrainScale;
    public byte surfaceBlock; // this value is changed by other scripts depending on planet
    public bool placeFlora = true;
    public Flora[] flora;
    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}

[System.Serializable]
public class Flora
{
    public string name;
    public int floraIndex;
    public float floraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float floraZoneThreshold = 0.6f;
    public float floraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float floraPlacementThreshold = 0.8f;
    public int minHeight = 5;
    public int maxHeight = 12;
    public int minRadius = 4;
    public int maxRadius = 8;
}