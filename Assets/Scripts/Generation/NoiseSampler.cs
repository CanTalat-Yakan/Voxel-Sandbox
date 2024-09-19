﻿using System.Buffers;

using LibNoise.Filter;
using LibNoise.Primitive;

using Engine.Utilities;

namespace VoxelSandbox;


public class NoiseSampler
{
    private static SimplexPerlin s_perlinPrimitive = new() { Seed = 1234 };

    private Billow _surfaceNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 8,        // Fewer octaves for a smoother look
        Frequency = 0.01f,      // Lower frequency for broader features
        Scale = 7.5f,           // Adjust scale to manage the level of detail
    };
    private Billow _undergroundNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 6,
        Frequency = 0.05f,
        Scale = 15f,
    };
    private Billow _caveNoise = new()
    {
        Primitive3D = s_perlinPrimitive,
        OctaveCount = 6,
        Frequency = 0.005f,
        Scale = 0.5f
    };

    private Random _random = new();

    private Dictionary<int, VoxelData[]> _rentedArrays = new();
    private ArrayPool<VoxelData> _pool = ArrayPool<VoxelData>.Shared;

    public const int BaseChunkSizeXZ = Generator.ChunkSizeXZ + 2;
    public const int BaseChunkSizeY = Generator.ChunkSizeY + 2;
    public const int BaseChunkTotalVoxelCount = BaseChunkSizeXZ * BaseChunkSizeXZ * BaseChunkSizeY;

    public void GenerateChunkContent(Chunk chunk)
    {
        int threadID = _random.Next(0, 100);
        var voxelArray = _pool.Rent(BaseChunkTotalVoxelCount / 2);
        _rentedArrays.Add(threadID, voxelArray);

        for (int x = 0; x <= Generator.ChunkSizeXZ + 1; x++)
            for (int z = 0; z <= Generator.ChunkSizeXZ + 1; z++)
            {
                int surfaceHeight = GetSurfaceHeight(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);
                int undergroundDetail = GetUndergroundDetail(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);

                for (int y = 0; y < Generator.ChunkSizeY; y++)
                    // Only generate solid voxels below the surface
                    if (y < surfaceHeight)
                    {
                        Vector3Byte voxelPosition = new(x, y, z);

                        // Check cave noise to determine if this voxel should be empty (cave)
                        if (y < undergroundDetail)
                            voxelArray[GetIndex(voxelPosition)].SetVoxel(voxelPosition, VoxelType.Stone);
                        else if (y + undergroundDetail < surfaceHeight)
                        {
                            double caveValue = GetCaveNoise(x + chunk.WorldPosition.X, y * 2, z + chunk.WorldPosition.Z);

                            if (caveValue < 0.25 || caveValue > 0.6)
                                continue;

                            voxelArray[GetIndex(voxelPosition)].SetVoxel(voxelPosition, VoxelType.Stone);
                        }
                        else if (y + undergroundDetail - 5 < surfaceHeight)
                            voxelArray[GetIndex(voxelPosition)].SetVoxel(voxelPosition, VoxelType.Stone);
                        else
                            voxelArray[GetIndex(voxelPosition)].SetVoxel(voxelPosition, VoxelType.Grass);
                    }
            }

        RemoveUnexposedVoxels(chunk, threadID);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);

        _pool.Return(voxelArray);
        _rentedArrays.Remove(threadID);
    }

    private int GetSurfaceHeight(int x, int z) =>
        (int)_surfaceNoise.GetValue(x, z) + 100;

    private int GetUndergroundDetail(int x, int z) =>
        (int)_undergroundNoise.GetValue(x, z) + 10;

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);

    private void RemoveUnexposedVoxels(Chunk chunk, int threadID)
    {
        var voxelArray = _rentedArrays[threadID];

        // Check if the voxel is exposed (has at least one neighbor that is empty)
        foreach (var voxel in voxelArray)
        {
            if (!voxel.Exists)
                continue;

            // Set border voxels as exposed
            if (!chunk.IsWithinBounds(voxel.Position))
            {
                chunk.VoxelData.Add(voxel.Position, voxel.Type);
                continue;
            }

            bool IsVoxelExposed = false;
            bool IsEdgeCaseExposed = IterateAdjacentVoxels(voxel.Position, (Vector3Byte adjacentLocalPosition) =>
            {
                // If the adjacent voxel is empty, the current voxel is exposed
                if (voxelArray[GetIndex(adjacentLocalPosition)].Empty && !IsVoxelExposed)
                    IsVoxelExposed = true;
            }, continueOnEdgeCases: false);

            if (IsEdgeCaseExposed || IsVoxelExposed)
                chunk.VoxelData.Add(voxel.Position, voxel.Type);
        }

        foreach (var voxel in chunk.VoxelData.Keys.ToArray())
            IterateAdjacentVoxels(voxel, (Vector3Byte adjacentLocalPosition) =>
            {
                if (!chunk.VoxelData.ContainsKey(adjacentLocalPosition))
                    if (voxelArray[GetIndex(adjacentLocalPosition)].Exists)
                        chunk.VoxelData.Add(adjacentLocalPosition, VoxelType.None);
                    else
                        chunk.VoxelData.Add(adjacentLocalPosition, VoxelType.Air);
            });
    }

    private bool IterateAdjacentVoxels(Vector3Byte localPosition, Action<Vector3Byte> action, bool continueOnEdgeCases = true)
    {
        Vector3Int adjacentVoxel = new();

        foreach (var direction in Vector3Int.Directions)
        {
            adjacentVoxel.X = direction.X + localPosition.X;
            adjacentVoxel.Y = direction.Y + localPosition.Y;
            adjacentVoxel.Z = direction.Z + localPosition.Z;

            if (adjacentVoxel.X <= 0 || adjacentVoxel.X >= Generator.ChunkSizeXZ + 1
             || adjacentVoxel.Z <= 0 || adjacentVoxel.Z >= Generator.ChunkSizeXZ + 1)
                if (continueOnEdgeCases) continue;

            // If the neighbor is outside the world bounds, consider it as empty
            if (adjacentVoxel.Y > Generator.ChunkSizeY)
                if (continueOnEdgeCases) continue;
                else return true;

            // If the neighbor is below ground, consider it as solid
            if (adjacentVoxel.Y < 0)
                if (continueOnEdgeCases) continue;
                else return false;

            // If the neighbor voxel is empty, the current voxel is exposed
            action.Invoke(new(adjacentVoxel.X, adjacentVoxel.Y, adjacentVoxel.Z));
        }

        // All neighbors are solid and the voxel is not exposed
        return false;
    }

    public static int GetIndex(Vector3Byte position) =>
        position.X + (BaseChunkSizeXZ * position.Z) + (BaseChunkSizeY * position.Y);
}