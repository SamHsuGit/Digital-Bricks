using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockBehavior
{
    public static bool Active (VoxelState voxel)
    {
        switch(voxel.id)
        {
            case 4: // Cloud (test)
            {
                if ((voxel.neighbors[0] != null && voxel.neighbors[0].id == 27) ||
                    (voxel.neighbors[1] != null && voxel.neighbors[1].id == 27) ||
                    (voxel.neighbors[4] != null && voxel.neighbors[4].id == 27) ||
                    (voxel.neighbors[5] != null && voxel.neighbors[5].id == 27))
                    {
                        return true;
                    }
                break;
            }
        }

        // If we get here, the block either isn't active or doesn't have a behavior. Just return false.
        return false;
    }

    public static void Behave(VoxelState voxel)
    {
        switch (voxel.id)
        {
            case 4: // Cloud (test)
                {
                    if (voxel.neighbors[2] != null && voxel.neighbors[2].id != 0)
                    {
                        voxel.chunkData.chunk.RemoveActiveVoxel(voxel);
                        voxel.chunkData.ModifyVoxel(voxel.position, 27, 0);
                        return;
                    }

                    List<VoxelState> neighbors = new List<VoxelState>();
                    if ((voxel.neighbors[0] != null && voxel.neighbors[0].id == 27)) neighbors.Add(voxel.neighbors[0]);
                    if ((voxel.neighbors[1] != null && voxel.neighbors[1].id == 27)) neighbors.Add(voxel.neighbors[1]);
                    if ((voxel.neighbors[4] != null && voxel.neighbors[4].id == 27)) neighbors.Add(voxel.neighbors[4]);
                    if ((voxel.neighbors[5] != null && voxel.neighbors[5].id == 27)) neighbors.Add(voxel.neighbors[5]);

                    if (neighbors.Count == 0) return;

                    int index = Random.Range(0, neighbors.Count);
                    neighbors[index].chunkData.ModifyVoxel(neighbors[index].position, 4, 0);

                    break;
                }
        }
    }
}
