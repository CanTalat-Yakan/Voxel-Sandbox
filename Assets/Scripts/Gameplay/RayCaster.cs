using System.Numerics;

using Engine.ECS;
using Engine.Utilities;

namespace VoxelSandbox
{
    public class RayCaster : Component
    {
        public Vector3Int? TargetVoxelPosition { get; private set; }
        public Vector3Int? AdjacentVoxelPosition { get; private set; }
        public Vector3 HitNormal { get; private set; }

        // Maximum distance the ray will check
        public float MaxDistance = 100f;

        private Entity _cube;
        private Entity _camera;

        public override void OnUpdate()
        {
            PerformRayCast();

            // Example usage: Update a cube's position to visualize the hit voxel
            if (TargetVoxelPosition is not null && _cube is not null)
                _cube.Transform.LocalPosition = TargetVoxelPosition.Value.ToVector3();

            if (Input.GetButton(MouseButton.Left, InputState.Down))
                if (TargetVoxelPosition is not null)
                    Generator.SetVoxel(TargetVoxelPosition.Value);
        }

        public void SetCube(Entity cube) =>
            _cube = cube;

        public void SetCamera(Entity camera) =>
            _camera = camera;

        private void PerformRayCast()
        {
            // Reset previous values
            TargetVoxelPosition = null;
            AdjacentVoxelPosition = null;
            HitNormal = Vector3.Zero;

            // Ensure _camera is assigned
            if (_camera == null)
                return;

            // Get the camera's position and direction
            Vector3 rayOrigin = _camera.Transform.LocalPosition;
            Vector3 rayDirection = _camera.Transform.Forward;

            // Normalize the ray direction
            rayDirection = Vector3.Normalize(rayDirection);

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
            float maxDistanceReached = 0f;

            while (maxDistanceReached < MaxDistance)
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
                        maxDistanceReached = tMax.X;
                        tMax.X += tDelta.X;
                        HitNormal = new Vector3(-rayStep.X, 0, 0);
                    }
                    else
                    {
                        voxelPos.Z += (int)rayStep.Z;
                        maxDistanceReached = tMax.Z;
                        tMax.Z += tDelta.Z;
                        HitNormal = new Vector3(0, 0, -rayStep.Z);
                    }
                }
                else
                {
                    if (tMax.Y < tMax.Z)
                    {
                        voxelPos.Y += (int)rayStep.Y;
                        maxDistanceReached = tMax.Y;
                        tMax.Y += tDelta.Y;
                        HitNormal = new Vector3(0, -rayStep.Y, 0);
                    }
                    else
                    {
                        voxelPos.Z += (int)rayStep.Z;
                        maxDistanceReached = tMax.Z;
                        tMax.Z += tDelta.Z;
                        HitNormal = new Vector3(0, 0, -rayStep.Z);
                    }
                }

                // Check if we've exceeded the maximum distance
                if (maxDistanceReached > MaxDistance)
                    break;
            }
        }

        private void InitializeDDAVariables(Vector3 rayOrigin, Vector3 rayDirection, ref Vector3Int voxelPos, ref Vector3 rayStep, ref Vector3 tMax, ref Vector3 tDelta)
        {
            // Calculate the direction of the step
            rayStep.X = Math.Sign(rayDirection.X);
            rayStep.Y = Math.Sign(rayDirection.Y);
            rayStep.Z = Math.Sign(rayDirection.Z);

            // Calculate tDelta
            tDelta.X = rayDirection.X == 0 ? float.MaxValue : Math.Abs(1f / rayDirection.X);
            tDelta.Y = rayDirection.Y == 0 ? float.MaxValue : Math.Abs(1f / rayDirection.Y);
            tDelta.Z = rayDirection.Z == 0 ? float.MaxValue : Math.Abs(1f / rayDirection.Z);

            // Calculate initial tMax
            float voxelBoundaryX = voxelPos.X + (rayStep.X > 0 ? 1f : 0f);
            float voxelBoundaryY = voxelPos.Y + (rayStep.Y > 0 ? 1f : 0f);
            float voxelBoundaryZ = voxelPos.Z + (rayStep.Z > 0 ? 1f : 0f);

            tMax.X = rayDirection.X == 0 ? float.MaxValue : (voxelBoundaryX - rayOrigin.X) / rayDirection.X;
            tMax.Y = rayDirection.Y == 0 ? float.MaxValue : (voxelBoundaryY - rayOrigin.Y) / rayDirection.Y;
            tMax.Z = rayDirection.Z == 0 ? float.MaxValue : (voxelBoundaryZ - rayOrigin.Z) / rayDirection.Z;
        }

        private bool IsVoxelSolid(Vector3Int voxelPosition)
        {
            // Retrieve the voxel at the given position
            Generator.GetChunkFromPosition(voxelPosition, out var chunk, out var localVoxelPosition);

            if (chunk is not null && chunk.SolidVoxelData is not null)
                if (chunk.IsVoxelSolid(ref localVoxelPosition))
                    return true;

            return false;
        }
    }
}