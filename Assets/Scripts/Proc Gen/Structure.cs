using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> GenerateSurfaceOb (int index, Vector3 position, int minHeight, int maxHeight, int minRadius, int maxRadius, float fertility, bool isEarth)
    {
        switch (index)
        {
            case 0:
                return MakeBaseVBOImport(position);
            //case 1:
            //    return MakeProcGenVBOImport(position); // decided not to allow VBO imports since is a performance hog, instead only spawn  imported object at base
            case 2:
                return MakeTree(position, minHeight, maxHeight, minRadius, maxRadius, fertility);
            case 3:
                return MakeCacti(position, minHeight, maxHeight, fertility);
            case 4:
                return MakeMushroomLarge(position, minHeight, maxHeight, minRadius, maxRadius, fertility);
            case 5:
                return MakeMonolith(position, minHeight, maxHeight, fertility);
            case 6:
                if (isEarth)
                    return MakeGrass(position); // only present on earth (green flora doesn't match on other planets)
                else
                    break;
            case 7:
                return MakeMushroomSmall(position); // must be present on all planets for gameplay (health)
            case 8:
                if (isEarth)
                    return MakeBamboo(position, fertility); // only present on earth (green flora doesn't match on other planets)
                else
                    break;
            case 9:
                if (isEarth)
                    return MakeFlower(position); // only present on earth (green flora doesn't match on other planets)
                else
                    break;
            case 10:
                return MakeEvergreen(position, minHeight, maxHeight, fertility);
            case 11:
                return MakeHoneyComb(position, minHeight, maxHeight, fertility);
            case 12:
                return MakeHugeTree(position);
            case 13:
                return MakeColumn(position);
            case 14:
                return MakeTreeFall(position, minHeight, maxHeight, minRadius, maxRadius, fertility);
            case 15:
                return MakeTreeSavanna(position, minHeight, maxHeight, fertility);
            case 16:
                return MakeRock(position);
            case 17:
                return MakeBoulder(position, fertility);
            case 18:
                return MakeShrub(position, fertility);
            case 19:
                return MakeTestRainbow(position);
        }
        return new Queue<VoxelMod>();
    }

    public static byte GetTreeBlockID(Vector3 treePosition, Vector3 voxelPosition, int floraRadius, float threshold)
    {
        byte blockID = PorousBlocks(floraRadius, treePosition, voxelPosition, World.Instance.worldData.blockIDTreeLeavesSummer, threshold); // leaves by default

        if (blockID == 0)
            return blockID;
        else
        {
            switch (World.Instance.season) // Calculate current season based on system date
            {
                case 1:
                    byte winter = World.Instance.worldData.blockIDTreeLeavesWinter;
                    return GetMixedBlockID(treePosition, voxelPosition, winter, winter, winter, winter); // Winter (Season1)
                case 2:
                    byte spring = World.Instance.worldData.blockIDTreeLeavesSpring;
                    return GetMixedBlockID(treePosition, voxelPosition, spring, spring, spring, spring); // Spring (Season 2)
                case 3:
                    byte summer = World.Instance.worldData.blockIDTreeLeavesSummer;
                    return GetMixedBlockID(treePosition, voxelPosition, summer, summer, summer, summer); // Summer (Season 3)
                case 4:
                    byte fall1 = World.Instance.worldData.blockIDTreeLeavesFall1;
                    byte fall2 = World.Instance.worldData.blockIDTreeLeavesFall2;
                    return GetMixedBlockID(treePosition, voxelPosition, fall1, fall2, fall1, fall2); // Fall (Season 4)
            }
            return 0;
        }
    }

    public static byte GetFallBlockID(Vector3 treePosition, Vector3 voxelPosition, int floraRadius, float threshold)
    {
        byte blockID = PorousBlocks(floraRadius, treePosition, voxelPosition, World.Instance.worldData.blockIDTreeLeavesSummer, threshold); // leaves by default

        if (blockID == 0)
            return blockID;
        else
        {
            byte fall1 = World.Instance.worldData.blockIDTreeLeavesFall1;
            byte fall2 = World.Instance.worldData.blockIDTreeLeavesFall2;
            return GetMixedBlockID(treePosition, voxelPosition, fall1, fall2, fall1, fall2);
        }
    }

    public static byte GetMixedBlockID(Vector3 treePosition, Vector3 voxelPosition, byte zero, byte one, byte two, byte three)
    {
        switch ((byte)Mathf.Clamp(Noise.Get2DPerlin(new Vector2(voxelPosition.x, voxelPosition.z), treePosition.x + treePosition.z, 0.8f) * 3, 0, 3))
        {
            case 0:
                return zero;
            case 1:
                return one;
            case 2:
                return two;
            case 3:
                return three;
        }
        return 0;
    }

    static void ReserveSpaceVBO(Queue<VoxelMod> queue, Vector3 position, int xRadius, int zRadius)
    {
        for (int x = -xRadius; x < xRadius; x++)
        {
            for (int z = -zRadius; z < zRadius; z++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight - position.y; y++)
                {
                    // + 1 to offset voxels to be aligned with center of plates
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y, position.z + z), 3)); // make stone 'baseplate' for model to sit on

                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y + 1, position.z + z), 0)); // reserve space for vboImport by creating air blocks in space it takes up
                }
            }
        }
    }

    static Queue<VoxelMod> MakeBaseVBOImport(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        LDrawImportRuntime.Instance.CalcBaseObSize(LDrawImportRuntime.Instance.baseOb);
        int xRadius = LDrawImportRuntime.Instance.baseObSizeX / 2;
        int zRadius = LDrawImportRuntime.Instance.baseObSizeZ / 2;
        xRadius += 1; // safety boundary in case import is offset by 1 block
        zRadius += 1; // safety boundary in case import is offset by 1 block
        byte blockID = 25;
        ReserveSpaceVBO(queue, position, xRadius, zRadius);
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + 1, position.z), blockID)); // add vboImport placeholder voxel to flag to world to add VBO
        return queue;
    }

    static Queue<VoxelMod> MakeTree(Vector3 position, int minHeight, int maxHeight, int minRadius, int maxRadius, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        int radius = (int)(maxRadius * _fertility);
        if (radius < minRadius)
            radius = minRadius;

        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDTreeTrunk)); // trunk

        for (int y = 0; y < 7; y++)
        {
            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    byte blockID = PorousBlocks(maxRadius, position, new Vector3(x,y,z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.515f); // leaves

                    if (blockID == 0)
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), blockID));
                    else
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.515f)));
                }
            }
        }

        return queue;
    }

    static Queue<VoxelMod> MakeTreeFall(Vector3 position, int minHeight, int maxHeight, int minRadius, int maxRadius, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        int radius = (int)(maxRadius * _fertility);
        if (radius < minRadius)
            radius = minRadius;

        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDTreeTrunk)); // trunk

        for (int y = 0; y < 7; y++)
        {
            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    byte blockID = PorousBlocks(maxRadius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.515f); // leaves

                    if (blockID == 0)
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), blockID));
                    else
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), GetFallBlockID(position, new Vector3(x, y, z), radius, 0.515f)));
                }
            }
        }

        return queue;
    }

    static Queue<VoxelMod> MakeTreeSavanna(Vector3 position, int minHeight, int maxHeight, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDTreeTrunk)); // trunk
        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x + 1, position.y + i + height / 2, position.z), World.Instance.worldData.blockIDTreeTrunk)); // trunk

        return queue;
    }

    static Queue<VoxelMod> MakeCacti(Vector3 position, int minHeight, int maxHeight, float _fertility)
    {

        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);

        if (height < minHeight)
            height = minHeight;

        for (int i = 1; i <= height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDCacti)); // cacti

        return queue;
    }

    static Queue<VoxelMod> MakeTestRainbow(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = 23;

        for(int i = 1; i <= height; i++)
        {
            byte blockID = (byte)(i + 1);
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y, position.z + i), blockID));
        }

        return queue;
    }

    static Queue<VoxelMod> MakeMushroomLarge(Vector3 position, int minHeight, int maxHeight, int minRadius, int maxRadius, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        int radius = (int)(maxRadius * _fertility);
        if (radius < minRadius)
            radius = minRadius;

        for (int i = 1; i < height + radius - 1; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDMushroomLargeStem)); // trunk

        for (int y = 0; y < radius; y++)
        {
            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    if (Mathf.Abs(x) == radius - y || Mathf.Abs(z) == radius - y || Mathf.Abs(x) == Mathf.Abs(z) && Mathf.Abs(x) == radius - 1 - y && Mathf.Abs(z) == radius - 1 - y)
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), World.Instance.worldData.blockIDMushroomLargeCap)); // mushroom top
                }
            }
        }
        return queue;
    }

    static Queue<VoxelMod> MakeMonolith(Vector3 position, int minHeight, int maxHeight, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        int radius = Mathf.FloorToInt(height / 3);
        if (radius < 1)
            radius = 1;

        for (int x = -radius; x < radius; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), World.Instance.worldData.blockIDMonolith)); // large black block
                }
            }
        }

        return queue;
    }

    static Queue<VoxelMod> MakeRock(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + 1, position.z), World.Instance.worldData.blockIDsubsurface)); // large black block
        return queue;
    }

    static Queue<VoxelMod> MakeBoulder(Vector3 position, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(3 * _fertility);
        int radius = (int)(5 * _fertility);

        for (int x = -radius; x < radius; x++)
        {
            for (int y = 1; y < height; y++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), World.Instance.worldData.blockIDsubsurface)); // large black block
                }
            }
        }

        return queue;
    }

    static Queue<VoxelMod> MakeShrub(Vector3 position, float fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(2 * fertility);
        int radius = (int)(3 * fertility);

        for (int x = -radius; x < radius; x++)
        {
            for (int y = 1; y < height; y++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), World.Instance.worldData.blockIDEvergreenLeaves)); // small shrub
                }
            }
        }
        return queue;
    }

    static Queue<VoxelMod> MakeGrass(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + 1, position.z), 31)); // grass
        return queue;
    }
    static Queue<VoxelMod> MakeMushroomSmall(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + 1, position.z), 32)); // mushroomSmall
        return queue;
    }

    static Queue<VoxelMod> MakeBamboo(Vector3 position, float fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(10 * fertility);

        for (int i = 1; i <= height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 33)); // bamboo

        return queue;

    }
    static Queue<VoxelMod> MakeFlower(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + 1, position.z), 34)); // flower
        return queue;
    }

    static Queue<VoxelMod> MakeEvergreen(Vector3 position, int minHeight, int maxHeight, float _fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * _fertility);
        if (height < minHeight)
            height = minHeight;

        int radius = Mathf.CeilToInt(height / 5);

        int yPrevious = 0;
        for (int y = 0; y < (height / 2) + 1; y += 2) // leaves (skips every 2 levels)
        {
            if (yPrevious + 4 == y) // subtracts from radius every 4
            {
                radius--;
                yPrevious = y;
            }

            if (radius < 0)
                radius = 0;

            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    if(Mathf.Abs(x) + Mathf.Abs(z) < radius)
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + Mathf.CeilToInt(height * 0.6f) + y, position.z + z), World.Instance.worldData.blockIDEvergreenLeaves)); // leaves
                }
            }
        }

        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDEvergreenTrunk)); // trunk

        return queue;
    }

    static Queue<VoxelMod> MakeHoneyComb(Vector3 position, int minHeight, int maxHeight, float fertility)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxHeight * fertility);
        if (height < minHeight)
            height = minHeight;

        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), World.Instance.worldData.blockIDHoneyComb)); // trunk

        return queue;
    }

    static Queue<VoxelMod> MakeHugeTree(Vector3 position)
    {
        position = new Vector3(position.x, position.y + 1, position.z); // ensure the large trees are above the ground
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = 50;
        int radius = Mathf.FloorToInt(height * 0.9f);

        for (int y = 0; y < Mathf.FloorToInt(height * 0.6f); y++)
        {
            radius-= 1;

            if (radius < 6)
                radius = 6;

            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDHugeTreeTrunk, 0.8f))); // trunk
                }
            }
        }

        for (int y = Mathf.FloorToInt(height * 0.6f); y < Mathf.FloorToInt(height * 0.8f); y++)
        {
            radius += 6;

            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    byte blockID = PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDHugeTreeLeaves, 0.7f); // leaves by default

                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.7f))); // leaves

                    float percentage = Mathf.Clamp(Noise.Get2DPerlin(new Vector2(x, z), 1, 20f), 0, 1); // hanging vines
                    if (blockID != 0 && percentage < 0.2f)
                    {
                        for (int i = height; i > height - height * percentage * 3; i--)
                        {
                            queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + i, position.z + z), PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.7f)));
                        }
                    }
                }
            }
        }

        for (int y = Mathf.FloorToInt(height * 0.8f); y < height; y++) // tree top
        {
            radius -= 6;

            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), PorousBlocks(radius, position, new Vector3(x, y, z), World.Instance.worldData.blockIDTreeLeavesSummer, 0.7f)));
                }
            }
        }

        return queue;
    }

    static Queue<VoxelMod> MakeColumn(Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int start = 0; // used to start column below surface level if want to hide under biome surface

        for (int i = start; i < (int)position.y; i++) // make column
        {
            byte blockID = World.Instance.worldData.blockIDColumn; // stone columns

            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y - i, position.z), blockID));
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y - i, position.z + 1), blockID));
            queue.Enqueue(new VoxelMod(new Vector3(position.x + 1, position.y - i, position.z), blockID));
            queue.Enqueue(new VoxelMod(new Vector3(position.x + 1, position.y - i, position.z + 1), blockID));
        }

        return queue;
    }

    public static byte PorousBlocks(float maxRadius, Vector3 treePosition, Vector3 voxelPosition, byte blockID, float threshold)
    {
        if (Noise.Get3DPerlin(voxelPosition, treePosition.x + treePosition.z, 0.5f, Mathf.Clamp((Mathf.Abs(voxelPosition.x) + Mathf.Abs(voxelPosition.z)) / maxRadius, 0, threshold)))
            return blockID;
        else
            return 0;
    }
}