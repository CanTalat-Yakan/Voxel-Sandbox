using System.Numerics;

using Engine.Components;
using Engine.ECS;
using Engine.Helper;
using Engine.Utilities;

namespace VoxelSandbox;

public class PlayerMovement : Component
{
    public float MovementSpeed = 10f;
    public float RotationSpeed = 50f;
    public float Gravitation = -9.8f;

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

        // Apply gravity
        if (!_isGrounded)
            _velocity.Y += Gravitation * Time.DeltaF;
        else
            _velocity.Y = 0;

        // Combine input direction with vertical velocity
        _velocity.X = inputDirection.X;
        _velocity.Z = inputDirection.Z;

        // Predict next position
        Vector3 nextPosition = Entity.Transform.LocalPosition + _velocity * Time.DeltaF;

        // Check for collision with ground
        if (_isGrounded = CheckGroundCollision(nextPosition, out float groundY))
        {
            // Snap to ground
            nextPosition.Y = groundY + 1 + 2; // Assuming the player is 2 units high
            _velocity.Y = 0;
        }

        // Update position
        Entity.Transform.LocalPosition = nextPosition;
    }

    private bool CheckGroundCollision(Vector3 position, out float groundY)
    {
        groundY = 0;
        Vector3Int voxelPosition = new Vector3Int(
            (int)Math.Floor(position.X),
            (int)Math.Floor(position.Y),
            (int)Math.Floor(position.Z));

        // Start checking from the voxel below the player's feet
        for (int y = voxelPosition.Y - 1; y >= 0; y--)
        {
            Vector3Int checkPosition = new Vector3Int(voxelPosition.X, y, voxelPosition.Z);
            Generator.GetChunkFromPosition(checkPosition, out var chunk, out var localVoxelPosition);

            if (chunk is not null && chunk.GetVoxel(localVoxelPosition, out var voxelType) && voxelType is not VoxelType.None)
            {
                groundY = y;
                return true;
            }
        }

        return false;
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
        if (!_cameraRotation.IsNaN())
        {
            Entity.Transform.EulerAngles = Vector3.UnitY * _cameraRotation.Y;

            if (Camera.Main is not null)
                Camera.Main.Entity.Transform.EulerAngles = Vector3.UnitX * _cameraRotation.X;
        }
    }
}
