using UnityEngine;

[CreateAssetMenu(fileName = "Planet", menuName = "ProcGen/Planet")]
public class Planet : ScriptableObject
{
    [Header("Planet Attributes")]
    public int distToStar;
    [Header("Surface Attributes")]
    public byte blockIDsubsurface;
    public byte blockIDcore;
    public byte blockIDBiome00;
    public byte blockIDBiome01;
    public byte blockIDBiome02;
    public byte blockIDBiome03;
    public byte blockIDBiome04;
    public byte blockIDBiome05;
    public byte blockIDBiome06;
    public byte blockIDBiome07;
    public byte blockIDBiome08;
    public byte blockIDBiome09;
    public byte blockIDBiome10;
    public byte blockIDBiome11;
    [Header("Flora Attributes")]
    public bool hasAtmosphere;
    public bool isAlive; // controls if the world is hospitable to flora
    public int[] biomes; // controls which biomes the world has
    public byte blockIDTreeLeavesWinter;
    public byte blockIDTreeLeavesSpring;
    public byte blockIDTreeLeavesSummer;
    public byte blockIDTreeLeavesFall1;
    public byte blockIDTreeLeavesFall2;
    public byte blockIDTreeTrunk;
    public byte blockIDCacti;
    public byte blockIDMushroomLargeCap;
    public byte blockIDMushroomLargeStem;
    public byte blockIDMonolith;
    public byte blockIDEvergreenLeaves;
    public byte blockIDEvergreenTrunk;
    public byte blockIDHoneyComb;
    public byte blockIDHugeTreeLeaves;
    public byte blockIDHugeTreeTrunk;
    public byte blockIDColumn;
}
