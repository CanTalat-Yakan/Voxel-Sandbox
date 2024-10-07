using System.Numerics;

using Vortice.DirectInput;

using Engine.ECS;
using Engine.Utilities;
using Engine.Components;

namespace VoxelSandbox;

public class PlayerController : Component
{
    public Camera Camera;

    // Movement and rotation settings
    public float MovementSpeed = 10f;
    public float RotationSpeed = 50f;

    // Physics settings
    public float Gravity = -20f;
    public float JumpForce = 7.5f;

    // Player dimensions
    public float PlayerHeight = 1.8f;
    public float PlayerWidth = 0.6f;
    public float PlayerDepth = 0.6f;

    // Step settings
    public float StepHeight = 0.6f;

    private Vector3 _velocity;
    private Vector3 _euler;

    private bool _isGrounded = false;

    public void Initialize(GameManager gameManager)
    {
        Camera = gameManager.Entity.Manager.CreateCamera(name: "Camera");
        Camera.FOV = 100;

        Entity.Transform.SetPosition(y: 1100);
        Entity.AddComponent<PlayerController>();
        Entity.AddComponent<RayCaster>().SetCamera(Camera.Entity);
    }

    public override void OnUpdate()
    {
        if (Camera is null)
            return;

        HandleRotation();
        HandleMovement();

        Camera.Entity.Transform.LocalPosition = Entity.Transform.Position + Vector3.UnitY * 1.8f;
    }

    private void HandleRotation()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();

        _euler.X = mouseDelta.Y;
        _euler.Y = mouseDelta.X;

        Entity.Transform.EulerAngles -= _euler * Time.DeltaF * RotationSpeed;

        // Clamp Vertical Rotation to ~90 degrees up and down.
        var clampedEuler = Entity.Transform.EulerAngles;
        clampedEuler.X = Math.Clamp(clampedEuler.X, -89, 89);
        Camera.Entity.Transform.EulerAngles = clampedEuler;
    }

    private void HandleMovement()
    {
        // Get input direction
        Vector3 inputDirection = Entity.Transform.Right * Input.GetAxis().X +
                                 Entity.Transform.Forward * Input.GetAxis().Y;

        // Normalize input direction and apply movement speed
        if (inputDirection != Vector3.Zero)
            inputDirection = Vector3.Normalize(inputDirection) * MovementSpeed;

        // Check for jump input
        if (Input.GetKey(Key.Space, InputState.Pressed) && _isGrounded)
        {
            _velocity.Y = JumpForce;
            _isGrounded = false;
        }

        // Apply gravity
        if (!_isGrounded)
            _velocity.Y += Gravity * Time.DeltaF;

        // Combine input direction with current vertical velocity
        _velocity.X = inputDirection.X;
        _velocity.Z = inputDirection.Z;

        // Predict next position
        Vector3 nextPosition = Entity.Transform.LocalPosition + _velocity * Time.DeltaF;

        // Handle collisions and get corrected position
        Vector3 correctedPosition = HandleCollisions(Entity.Transform.LocalPosition, nextPosition);

        // Update position directly
        Entity.Transform.LocalPosition = correctedPosition;
    }

    private Vector3 HandleCollisions(Vector3 currentPosition, Vector3 nextPosition)
    {
        Vector3 finalPosition = nextPosition;

        // Check vertical collisions
        finalPosition = HandleVerticalCollisions(currentPosition, finalPosition);

        // Check horizontal collisions and step smoothing
        finalPosition = HandleHorizontalCollisions(currentPosition, finalPosition);

        return finalPosition;
    }

    private Vector3 HandleVerticalCollisions(Vector3 currentPosition, Vector3 nextPosition)
    {
        Vector3 finalPosition = nextPosition;

        float playerBottom = nextPosition.Y;
        float playerTop = nextPosition.Y + PlayerHeight;

        // Check for ground collision
        if (_velocity.Y <= 0) // Falling or moving down
        {
            if (IsCollidingAtPosition(new Vector3(nextPosition.X, playerBottom - 0.1f, nextPosition.Z)))
            {
                _isGrounded = true;
                _velocity.Y = 0;

                // Determine the Y position of the ground voxel
                float groundY = (float)Math.Floor(playerBottom - 0.1f) + 1f;

                // Set the player's position so that their feet are on top of the ground voxel
                finalPosition.Y = groundY;
            }
            else
                _isGrounded = false;
        }
        // Check for ceiling collision
        else if (_velocity.Y > 0)
        {
            if (IsCollidingAtPosition(new Vector3(nextPosition.X, playerTop + 0.1f, nextPosition.Z)))
            {
                _velocity.Y = 0;

                // Determine the Y position of the ceiling voxel
                float ceilingY = (float)Math.Floor(playerTop + 0.1f);

                // Set the player's position so that their head is just below the ceiling voxel
                finalPosition.Y = ceilingY - PlayerHeight;
            }
        }

        return finalPosition;
    }

    private Vector3 HandleHorizontalCollisions(Vector3 currentPosition, Vector3 nextPosition)
    {
        Vector3 finalPosition = nextPosition;

        // Horizontal movement vector
        Vector3 horizontalMovement = new Vector3(_velocity.X, 0, _velocity.Z) * Time.DeltaF;

        // Check for obstacles in the movement direction
        if (horizontalMovement != Vector3.Zero)
        {
            Vector3 proposedPosition = new Vector3(nextPosition.X, currentPosition.Y, nextPosition.Z);

            if (IsCollidingAtPosition(proposedPosition))
            {
                // Attempt to step up
                if (AttemptStepUp(currentPosition, horizontalMovement, out Vector3 steppedPosition))
                {
                    // Move to the stepped position
                    finalPosition = steppedPosition;
                    _isGrounded = true;
                }
                else
                {
                    // Can't move forward, stop horizontal movement
                    _velocity.X = 0;
                    _velocity.Z = 0;
                    finalPosition.X = currentPosition.X;
                    finalPosition.Z = currentPosition.Z;
                }
            }
        }

        return finalPosition;
    }

    private bool AttemptStepUp(Vector3 currentPosition, Vector3 horizontalMovement, out Vector3 steppedPosition)
    {
        steppedPosition = currentPosition;

        float distance = horizontalMovement.Length();
        Vector3 direction = Vector3.Normalize(horizontalMovement);

        // Check if we can step up by StepHeight
        for (float yOffset = 0.05f; yOffset <= StepHeight; yOffset += 0.05f)
        {
            Vector3 newPosition = currentPosition + new Vector3(0, yOffset, 0) + direction * distance;

            // Check if the space is free at the new position
            if (!IsCollidingAtPosition(newPosition))
            {
                steppedPosition = newPosition;
                return true;
            }
        }

        return false;
    }

    private bool IsCollidingAtPosition(Vector3 position)
    {
        // Define the player's AABB at the given position
        float halfWidth = PlayerWidth / 2f;
        float halfDepth = PlayerDepth / 2f;

        Vector3 playerMin = new Vector3(position.X - halfWidth, position.Y, position.Z - halfDepth);
        Vector3 playerMax = new Vector3(position.X + halfWidth, position.Y + PlayerHeight, position.Z + halfDepth);

        // Loop through all voxels that the bounding box could potentially intersect
        for (int x = (int)Math.Floor(playerMin.X); x <= (int)Math.Floor(playerMax.X); x++)
            for (int y = (int)Math.Floor(playerMin.Y); y <= (int)Math.Floor(playerMax.Y); y++)
                for (int z = (int)Math.Floor(playerMin.Z); z <= (int)Math.Floor(playerMax.Z); z++)
                {
                    Vector3Int voxelPosition = new Vector3Int(x, y, z);
                    Generator.GetChunkFromPosition(voxelPosition, out var chunk, out var localVoxelPosition);

                    if (chunk is not null && chunk.SolidVoxelData is not null)
                        if (chunk.IsVoxelSolid(ref localVoxelPosition))
                        {
                            // Define the voxel's AABB
                            Vector3 voxelMin = new Vector3(x, y, z);
                            Vector3 voxelMax = voxelMin + new Vector3(1, 1, 1);

                            // Check for AABB intersection
                            if (AABBIntersects(playerMin, playerMax, voxelMin, voxelMax))
                            {
                                // Collision detected
                                return true;
                            }
                        }
                }

        // No collision detected
        return false;
    }

    private bool AABBIntersects(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return (minA.X <= maxB.X && maxA.X >= minB.X) &&
               (minA.Y <= maxB.Y && maxA.Y >= minB.Y) &&
               (minA.Z <= maxB.Z && maxA.Z >= minB.Z);
    }
}
