using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New BlockType", menuName = "ProcGen/BlockType")]
public class BlockType : ScriptableObject
{
    public string blockName;
    public int stackMax;
    public bool isDrawn;
    public bool isSolid;
    public bool isTransparent;
    public bool isWater;
    public int hardness;
    public VoxelMeshData standardMeshData;
    public VoxelMeshData studsMeshData;
    public Sprite icon;
    public bool isActive;
    public GameObject voxelBoundObject;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    public int topFaceSmoothTexture;

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
