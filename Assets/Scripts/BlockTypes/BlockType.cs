using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New BlockType", menuName = "ProcGen/BlockType")]
public class BlockType : ScriptableObject
{
    public string blockName;
    public byte id;
    public byte dropID;
    public int stackMax;
    public bool isDrawn;
    public bool isSolid;
    public bool isTransparent;
    public bool isWater;
    public float hardness;
    public VoxelMeshData standardMeshData;
    public VoxelMeshData studsMeshData;
    public Sprite icon;
    public string colorHexValue;
    public int ldrawHexValueCodeNumber;
    public Material material;
    public bool isActive;
    public GameObject voxelBoundObject; // what prefab should be rendered in place of this voxel?
    public GameObject voxelPrefab; // what prefab should be displayed when grabbing this voxel?
    public GameObject voxelBitPrefab; // what color bit should be shown when shoot this voxel? (not currently used, could not grab from sceneObject from World)

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    public int topFaceSmoothTexture;
    public AudioClip[] blockSounds;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            case 6:
                return topFaceSmoothTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}
