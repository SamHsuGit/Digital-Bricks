using UnityEngine;

public static class VoxelData
{
	public static readonly int ChunkWidth = 16; // Lego World Chunk = 32x32 studs = 16x16 blocks
	public static readonly int ChunkHeight = 96; // original Minecraft World Height Limit = 128, found that a smaller chunkHeight is needed to reduce world load times to under 15 seconds
	public static readonly int WorldSizeInChunks = 5; // was 5000, reduced since limiting choices unleashes creativity. Lego Worlds "Medium" world size = 100x100 chunks, 5000x16 = 80,000 bricks (meters) long / 25 mps = 3,200s to fly across world (1,600s from center to border)
	public static readonly float voxelWidth = 1.0f;
	public static readonly float voxelHeight = 1.0f;
	public static readonly float scale = 1.0f;

	public static int WorldSizeInVoxels
	{
		get { return WorldSizeInChunks * ChunkWidth; }
	}

	public static readonly int TextureAtlasSizeInBlocks = 16;
	public static float NormalizedBlockTextureSize
	{
		get { return 1f / (float)TextureAtlasSizeInBlocks; }
	}

	public static readonly Vector3[] voxelVerts = new Vector3[8] {
		// Updated to use voxelHeight and voxelWidth
		new Vector3(0.0f, 0.0f, 0.0f),
		new Vector3(voxelWidth, 0.0f, 0.0f),
		new Vector3(voxelWidth, voxelHeight, 0.0f),
		new Vector3(0.0f, voxelHeight, 0.0f),
		new Vector3(0.0f, 0.0f, voxelWidth),
		new Vector3(voxelWidth, 0.0f, voxelWidth),
		new Vector3(voxelWidth, voxelHeight, voxelWidth),
		new Vector3(0.0f, voxelHeight, voxelWidth),
	};

	public static readonly Vector3[] faceChecks = new Vector3[6] {
		// Updated to use voxelHeight and voxelWidth
		new Vector3(0.0f, 0.0f, -voxelWidth),
		new Vector3(0.0f, 0.0f, voxelWidth),
		new Vector3(0.0f, voxelHeight, 0.0f),
		new Vector3(0.0f, -voxelHeight, 0.0f),
		new Vector3(-voxelWidth, 0.0f, 0.0f),
		new Vector3(voxelWidth, 0.0f, 0.0f)
	};

	public static readonly int[,] voxelTris = new int[6, 4] {
        // Back, Front, Top, Bottom, Left, Right

		// 0 1 2 2 1 3
		{0, 3, 1, 2}, // Back Face
		{5, 6, 4, 7}, // Front Face
		{3, 7, 2, 6}, // Top Face
		{1, 5, 0, 4}, // Bottom Face
		{4, 7, 0, 3}, // Left Face
		{1, 2, 5, 6} // Right Face
	};

	public static readonly Vector2[] voxelUvs = new Vector2[4] {
		new Vector2 (0.0f, 0.0f),
		new Vector2 (0.0f, 1.0f),
		new Vector2 (1.0f, 0.0f),
		new Vector2 (1.0f, 1.0f)
	};
}