using System.Numerics;

using Engine.Components;
using Engine.ECS;

namespace VoxelSandbox;

public class RayCaster : Component
{
    public Vector3Int? TargetVoxelPosition { get; private set; }
    public Vector3Int? AdjacentVoxelPosition { get; private set; }
    public Vector3 HitNormal { get; private set; }

    // Maximum distance the ray will check
    public float MaxDistance = 10f;

    private Entity _cube;
    private Entity _camera;

    public override void OnStart()
    {
        _cube = Entity.Manager.CreatePrimitive().Entity;
        _cube.Transform.LocalScale *= 1.1f;
    }

    public override void OnUpdate()
    {
        PerformRayCast();

        if (TargetVoxelPosition is not null)
            _cube.Transform.LocalPosition = TargetVoxelPosition.Value.ToVector3();
    }

    public void SetCamera(Entity camera) =>
        _camera = camera;

    private void PerformRayCast()
    {
        // Reset previous values
        TargetVoxelPosition = null;
        AdjacentVoxelPosition = null;
        HitNormal = Vector3.Zero;

        // Get the camera's position and direction
        Vector3 rayOrigin = _camera.Transform.LocalPosition;
        Vector3 rayDirection = _camera.Transform.Forward;

        // Implement the DDA algorithm
        Vector3Int voxelPos = new Vector3Int(
            (int)Math.Floor(rayOrigin.X),
            (int)Math.Floor(rayOrigin.Y),
            (int)Math.Floor(rayOrigin.Z)
        );

        Vector3 rayStep = Vector3.Zero;
        Vector3 tMax = Vector3.Zero;
        Vector3 tDelta = Vector3.Zero;

        // Determine the step direction and initial tMax and tDelta values
        InitializeDDAVariables(rayOrigin, rayDirection, ref voxelPos, ref rayStep, ref tMax, ref tDelta);

        // Traverse the voxel grid
        for (int i = 0; i < (int)(MaxDistance * 2); i++)
        {
            // Check if the current voxel contains a block
            if (IsVoxelSolid(voxelPos))
            {
                TargetVoxelPosition = voxelPos;
                AdjacentVoxelPosition = voxelPos + new Vector3Int(
                    (int)-rayStep.X,
                    (int)-rayStep.Y,
                    (int)-rayStep.Z
                );
                HitNormal = new Vector3(
                    (int)-rayStep.X,
                    (int)-rayStep.Y,
                    (int)-rayStep.Z
                );
                break;
            }

            // Move to the next voxel
            if (tMax.X < tMax.Y)
            {
                if (tMax.X < tMax.Z)
                {
                    voxelPos.X += (int)rayStep.X;
                    tMax.X += tDelta.X;
                }
                else
                {
                    voxelPos.Z += (int)rayStep.Z;
                    tMax.Z += tDelta.Z;
                }
            }
            else
            {
                if (tMax.Y < tMax.Z)
                {
                    voxelPos.Y += (int)rayStep.Y;
                    tMax.Y += tDelta.Y;
                }
                else
                {
                    voxelPos.Z += (int)rayStep.Z;
                    tMax.Z += tDelta.Z;
                }
            }

            // Check if we've exceeded the maximum distance
            float distanceTraveled = Vector3.Distance(rayOrigin, voxelPos.ToVector3());
            if (distanceTraveled > MaxDistance)
                break;
        }
    }

    private void InitializeDDAVariables(Vector3 rayOrigin, Vector3 rayDirection, ref Vector3Int voxelPos, ref Vector3 rayStep, ref Vector3 tMax, ref Vector3 tDelta)
    {
        // Normalize the ray direction
        rayDirection = Vector3.Normalize(rayDirection);

        // Calculate the direction of the step
        rayStep.X = Math.Sign(rayDirection.X);
        rayStep.Y = Math.Sign(rayDirection.Y);
        rayStep.Z = Math.Sign(rayDirection.Z);

        // Calculate tDelta
        tDelta.X = rayDirection.X == 0 ? float.MaxValue : Math.Abs(1 / rayDirection.X);
        tDelta.Y = rayDirection.Y == 0 ? float.MaxValue : Math.Abs(1 / rayDirection.Y);
        tDelta.Z = rayDirection.Z == 0 ? float.MaxValue : Math.Abs(1 / rayDirection.Z);

        // Calculate initial tMax
        float voxelBoundaryX = voxelPos.X + (rayStep.X > 0 ? 1 : 0);
        float voxelBoundaryY = voxelPos.Y + (rayStep.Y > 0 ? 1 : 0);
        float voxelBoundaryZ = voxelPos.Z + (rayStep.Z > 0 ? 1 : 0);

        tMax.X = rayDirection.X == 0 ? float.MaxValue : (voxelBoundaryX - rayOrigin.X) / rayDirection.X;
        tMax.Y = rayDirection.Y == 0 ? float.MaxValue : (voxelBoundaryY - rayOrigin.Y) / rayDirection.Y;
        tMax.Z = rayDirection.Z == 0 ? float.MaxValue : (voxelBoundaryZ - rayOrigin.Z) / rayDirection.Z;
    }

    private bool IsVoxelSolid(Vector3Int voxelPosition)
    {
        // Retrieve the voxel at the given position
        Generator.GetChunkFromPosition(voxelPosition, out var chunk, out var localVoxelPosition);

        if (chunk != null && chunk.GetVoxel(localVoxelPosition, out var voxelType))
            return voxelType != VoxelType.None;

        return false;
    }
}