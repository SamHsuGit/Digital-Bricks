using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        offset += SettingsStatic.LoadedSettings.seed; // make entire world generation affected by single seed

        float y = 0;
        // purpose of 0.1f is to make not a whole number since all positions will be whole numbers
        y = Mathf.PerlinNoise((position.x + 0.1f) / VoxelData.ChunkWidth * scale + offset, (position.y + 0.1f) / VoxelData.ChunkWidth * scale + offset);

        // multi-octave perlin noise is too resource intensive to be used
        //float[] octaveFrequencies = new float[] { 1.0f, 1.5f };
        //float[] octaveAmplitudes = new float[] { 1.0f, 0.9f };
        //for (int i = 0; i < octaveFrequencies.Length;i++)
        //{
        //    y += octaveAmplitudes[i] * Mathf.PerlinNoise(octaveFrequencies[i] * (position.x + 0.1f) / VoxelData.ChunkWidth * scale + offset, octaveFrequencies[i] * (position.y + 0.1f) / VoxelData.ChunkWidth * scale + offset);
        //}

        return y;
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        offset += SettingsStatic.LoadedSettings.seed; // make entire world generation affected by single seed

        // https://www.youtube.com/watch?v=Aga0TBJkchM Carpilot on YouTube

        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }

}