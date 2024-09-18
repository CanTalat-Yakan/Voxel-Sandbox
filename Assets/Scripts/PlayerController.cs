using System.Numerics;

using Vortice.DirectInput;

using Engine.Components;
using Engine.ECS;
using Engine.Helper;
using Engine.Utilities;

namespace VoxelSandbox;

public class PlayerMovement : Component
{
    public float MovementSpeed = 10f;
    public float RotationSpeed = 50f;
    public float Gravity = -20f;    
    public float JumpForce = 10f;
    public float StepHeight = 0.6f; 
    public float PlayerHeight = 1.8f;

    private Vector3 _velocity;
    private Vector2 _cameraRotation;

    private bool _isGrounded = false;

    public override void OnUpdate()
    {
        HandleMovement();
        HandleRotation();
        UpdateTransform();
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

        float playerBottom = nextPosition.Y - PlayerHeight;
        float playerTop = nextPosition.Y + PlayerHeight;

        // Check for ground collision
        if (_velocity.Y <= 0) // Falling or moving down
        {
            if (CheckVoxelCollision(new Vector3(nextPosition.X, playerBottom - 0.1f, nextPosition.Z)))
            {
                _isGrounded = true;
                _velocity.Y = 0;

                // Determine the Y position of the ground voxel
                float groundY = (float)Math.Floor(playerBottom - 0.1f) + 1f;

                // Set the player's position so that their feet are on top of the ground voxel
                finalPosition.Y = groundY + PlayerHeight;
            }
            else
                _isGrounded = false;
        }
        // Check for ceiling collision
        else if (_velocity.Y > 0)
        {
            if (CheckVoxelCollision(new Vector3(nextPosition.X, playerTop + 0.1f, nextPosition.Z)))
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
            Vector3 direction = Vector3.Normalize(horizontalMovement);
            float distance = horizontalMovement.Length();

            // Cast a ray in the movement direction
            if (CheckVoxelCollision(currentPosition + direction * distance))
            {
                // Attempt to step up
                if (AttemptStepUp(currentPosition, direction, out Vector3 steppedPosition))
                    finalPosition = steppedPosition;
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

    private bool AttemptStepUp(Vector3 currentPosition, Vector3 direction, out Vector3 steppedPosition)
    {
        steppedPosition = currentPosition;

        // Check if we can step up by StepHeight
        for (float yOffset = 0.05f; yOffset <= StepHeight; yOffset += 0.05f)
        {
            Vector3 newPosition = currentPosition + new Vector3(0, yOffset, 0) + direction * (_velocity * Time.DeltaF).Length();

            // Check if the space is free
            if (!CheckVoxelCollision(newPosition))
            {
                steppedPosition = newPosition;
                return true;
            }
        }

        return false;
    }

    private bool CheckVoxelCollision(Vector3 position)
    {
        Vector3Int voxelPosition = new Vector3Int(
            (int)Math.Floor(position.X),
            (int)Math.Floor(position.Y),
            (int)Math.Floor(position.Z));

        Generator.GetChunkFromPosition(voxelPosition, out var chunk, out var localVoxelPosition);

        if (chunk != null && chunk.GetVoxel(localVoxelPosition, out var voxelType) && voxelType != VoxelType.None)
            return true; // Collision detected

        return false; // No collision
    }

    private void HandleRotation()
    {
        if (!Input.GetButton(MouseButton.Right))
            return;

        var mouseInput = Input.GetMouseDelta();
        _cameraRotation.Y -= mouseInput.X * RotationSpeed * Time.DeltaF;
        _cameraRotation.X -= mouseInput.Y * RotationSpeed * Time.DeltaF;
        _cameraRotation.X = Math.Clamp(_cameraRotation.X, -89f, 89f);
    }

    private void UpdateTransform()
    {
        // Update rotation
        if (_cameraRotation.IsNaN())
            return;

        Entity.Transform.EulerAngles = Vector3.UnitY * _cameraRotation.Y;

        if (Camera.Main != null)
        {
            // Set camera rotation
            Camera.Main.Entity.Transform.EulerAngles = Vector3.UnitX * _cameraRotation.X;

            // Adjust camera position relative to player
            Camera.Main.Entity.Transform.LocalPosition = Entity.Transform.LocalPosition;
        }
    }
}
