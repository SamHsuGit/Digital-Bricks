using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Planet", menuName = "ProcGen/Planet")]
public class Planet : ScriptableObject
{
    [Header("Planet Attributes")]
    public int distToStar;
    [Header("Surface Attributes")]
    public byte blockIDsubsurface;
    public byte blockIDcore;
    public byte blockIDForest;
    public byte blockIDGrasslands;
    public byte blockIDDesert;
    public byte blockIDDeadForest;
    public byte blockIDHugeTree;
    public byte blockIDMountain;
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
