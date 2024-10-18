using System.Numerics;

using Engine.ECS;
using Engine.Utilities;
using Engine.Components;

namespace VoxelSandbox
{
    public class PlayerController : Component
    {
        public CharacterCollider CharacterCollider = new();

        public Camera Camera;

        // Movement and rotation settings
        public float MovementSpeed = 10f;
        public float RotationSpeed = 50f;

        // Physics settings
        public float Gravity = -20f;
        public float JumpForce = 7.5f;

        private Vector3 _velocity;
        private Vector3 _euler;

        private bool _isGrounded = false;

        public void Initialize(GameManager gameManager)
        {
            Entity.Transform.SetPosition(y: 1100);

            var cube = gameManager.Entity.Manager.CreateEntity().AddComponent<Mesh>();
            cube.SetMeshData(Assets.Meshes["Cube.obj"]);
            cube.SetRootSignature();
            cube.SetMaterialPipeline("SimpleLit");
            cube.SetMaterialTextures([new("Transparent.png", 0)]);
            cube.Order = byte.MaxValue;

            Camera = gameManager.Entity.Manager.CreateCamera(name: "Camera");
            Camera.Entity.Transform.SetPosition(y: 1100);

            Entity.AddComponent<RayCaster>().Initialize(gameManager, cube.Entity, Camera.Entity);

            CharacterCollider.Initialize(gameManager);
        }

        public override void OnUpdate()
        {
            if (Camera is null)
                return;

            HandleRotation();
            HandleMovement();

            Camera.Entity.Transform.LocalPosition = Entity.Transform.Position + Vector3.UnitY * CharacterCollider.PlayerHeight;
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
            else
                _velocity.Y = 0; // Ensure vertical velocity is zero when grounded

            // Combine input direction with current vertical velocity
            _velocity.X = inputDirection.X;
            _velocity.Z = inputDirection.Z;

            // Desired movement for this frame
            Vector3 desiredMovement = _velocity * Time.DeltaF;

            // Update collider's velocity reference
            CharacterCollider.Velocity = _velocity;

            // Use the collider to move and get the final position
            Vector3 finalPosition = CharacterCollider.Move(Entity.Transform.LocalPosition, desiredMovement);

            if (finalPosition.Y < 0)
                finalPosition.Y = 1200;

            // Update position directly
            Entity.Transform.LocalPosition = finalPosition;

            // Update _velocity in case it was modified during collision handling
            _velocity = CharacterCollider.Velocity;

            // Update grounded state
            _isGrounded = CharacterCollider.IsGrounded;
        }
    }
}