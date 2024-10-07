using System.Numerics;

using Engine.ECS;

namespace VoxelSandbox;

public class CharacterCollider : Component
{
    // Player dimensions
    public float PlayerHeight = 1.8f;
    public float PlayerWidth = 0.6f;
    public float PlayerDepth = 0.6f;

    // Reference to the player's velocity, to apply adjustments
    public Vector3 Velocity;

    public bool IsGrounded { get; private set; }

    public Vector3 Move(Vector3 currentPosition, Vector3 desiredMovement)
    {
        Vector3 finalPosition = currentPosition;

        // Apply movement step by step, checking collisions
        IsGrounded = false;

        // Split movement into horizontal and vertical components
        Vector3 horizontalMovement = new(desiredMovement.X, 0, desiredMovement.Z);
        float verticalMovement = desiredMovement.Y;

        // First handle vertical movement (Y axis)
        finalPosition = MoveVertical(finalPosition, verticalMovement);

        // Then handle horizontal movement, with sliding
        finalPosition = MoveHorizontal(finalPosition, horizontalMovement);

        return finalPosition;
    }

    private Vector3 MoveVertical(Vector3 position, float movementY)
    {
        Vector3 finalPosition = position;

        if (movementY != 0)
        {
            Vector3 nextPosition = position + Vector3.UnitY * movementY;

            if (!IsCollidingAtPosition(nextPosition))
                finalPosition = nextPosition;
            else
            {
                // Collision occurred
                if (movementY < 0)
                    // Moving downwards, so we are grounded
                    IsGrounded = true;

                // Stop vertical movement
                Velocity.Y = 0;
            }
        }
        else
        {
            // No vertical movement, check if grounded
            Vector3 checkPosition = position + Vector3.UnitY * -0.1f;

            if (IsCollidingAtPosition(checkPosition))
                IsGrounded = true;
        }

        return finalPosition;
    }

    private Vector3 MoveHorizontal(Vector3 position, Vector3 movement)
    {
        Vector3 finalPosition = position;

        // Attempt to move along X axis
        if (movement.X != 0)
        {
            Vector3 moveX = Vector3.UnitX * movement.X;
            Vector3 nextPositionX = finalPosition + moveX;

            if (!IsCollidingAtPosition(nextPositionX))
                finalPosition += moveX;
            else
                // Collision in X axis, zero out X component of velocity
                Velocity.X = 0;
        }

        // Attempt to move along Z axis
        if (movement.Z != 0)
        {
            Vector3 moveZ = Vector3.UnitZ * movement.Z;
            Vector3 nextPositionZ = finalPosition + moveZ;

            if (!IsCollidingAtPosition(nextPositionZ))
                finalPosition += moveZ;
            else
                // Collision in Z axis, zero out Z component of velocity
                Velocity.Z = 0;
        }

        return finalPosition;
    }

    private bool IsCollidingAtPosition(Vector3 position)
    {
        // Define the player's AABB at the given position
        float halfWidth = PlayerWidth / 2f;
        float halfDepth = PlayerDepth / 2f;

        Vector3 playerMin = new(position.X - halfWidth, position.Y, position.Z - halfDepth);
        Vector3 playerMax = new(position.X + halfWidth, position.Y + PlayerHeight, position.Z + halfDepth);

        // Loop through all voxels that the bounding box could potentially intersect
        for (int x = (int)Math.Floor(playerMin.X); x <= (int)Math.Floor(playerMax.X); x++)
            for (int y = (int)Math.Floor(playerMin.Y); y <= (int)Math.Floor(playerMax.Y); y++)
                for (int z = (int)Math.Floor(playerMin.Z); z <= (int)Math.Floor(playerMax.Z); z++)
                {
                    Vector3Int voxelPosition = new(x, y, z);
                    Generator.GetChunkFromPosition(voxelPosition, out var chunk, out var localVoxelPosition);

                    if (chunk is not null && chunk.SolidVoxelData is not null)
                        if (chunk.IsVoxelSolid(ref localVoxelPosition))
                        {
                            // Define the voxel's AABB
                            Vector3 voxelMin = new(x, y, z);
                            Vector3 voxelMax = voxelMin + Vector3.One;

                            // Check for AABB intersection
                            if (AABBIntersects(playerMin, playerMax, voxelMin, voxelMax))
                                // Collision detected
                                return true;
                        }
                }

        // No collision detected
        return false;
    }

    private bool AABBIntersects(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB) =>
        (minA.X <= maxB.X && maxA.X >= minB.X)
     && (minA.Y <= maxB.Y && maxA.Y >= minB.Y)
     && (minA.Z <= maxB.Z && maxA.Z >= minB.Z);
}
