using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New BlockProperties", menuName = "ProcGen/BlockProperties")]
public class BlockProperties : ScriptableObject
{
    public int stackMax;
    public bool isDrawn;
    public bool isSolid;
    public bool isTransparent;
    public bool isWater;
    public VoxelMeshData standardMeshData;
    public VoxelMeshData studsMeshData;
    public AudioClip[] blockSounds;
    public AudioClip pickupSound;
}
