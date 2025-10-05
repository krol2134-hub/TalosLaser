using KinematicCharacterController;
using UnityEngine;

namespace TalosTest.Character
{
    public class PlayerController : ICharacterController
    {
        private readonly KinematicCharacterMotor _motor;
        private readonly Interactor _interactor;
        private readonly PlayerInput _input;
        private readonly PlayerSettings _settings;

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;

        private bool _jumpRequested;
        private bool _jumpConsumed;
        private bool _jumpedThisFrame;
        private bool _doubleJumpConsumed;

        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump;

        public Quaternion CameraRotation { get; set; } = Quaternion.identity;

        public PlayerController(KinematicCharacterMotor motor, Interactor interactor, PlayerInput input, PlayerSettings settings)
        {
            _motor = motor;
            _interactor = interactor;
            _input = input;
            _settings = settings;
            _motor.CharacterController = this;
        }

        public void Update()
        {
            var inputs = new PlayerCharacterInputs
            {
                MoveAxisForward = _input.Move.y,
                MoveAxisRight = _input.Move.x,
                CameraRotation = CameraRotation,
                JumpDown = _input.Jump,
                Interact = _input.Interact
            };

            ApplyInputs(ref inputs);
        }

        private void ApplyInputs(ref PlayerCharacterInputs inputs)
        {
            var moveInput = new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward);
            _moveInputVector = Vector3.ClampMagnitude(moveInput, 1f);

            var cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, _motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, _motor.CharacterUp).normalized;

            var cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, _motor.CharacterUp);
            _moveInputVector = cameraPlanarRotation * _moveInputVector;
            _lookInputVector = cameraPlanarDirection;

            if (inputs.JumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }

            if (inputs.Interact)
            {
                _interactor?.HandleInteractInput();
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;

            if (_motor.GroundingStatus.IsStableOnGround)
            {
                UpdateGroundMovement(ref currentVelocity, deltaTime);
            }
            else
            {
                UpdateAirMovement(ref currentVelocity, deltaTime);
            }

            TryJump(ref currentVelocity);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector == Vector3.zero || _settings.OrientationSharpness <= 0f)
                return;

            var progress = 1f - Mathf.Exp(-_settings.OrientationSharpness * deltaTime);
            var smoothedLookDirection = Vector3.Slerp(_motor.CharacterForward, _lookInputVector, progress).normalized;

            currentRotation = Quaternion.LookRotation(smoothedLookDirection, _motor.CharacterUp);
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            UpdateJumpTimers(deltaTime);
        }

        private void UpdateGroundMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            var inputRight = Vector3.Cross(_moveInputVector, _motor.CharacterUp);
            var reorientedInput = Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            var targetVelocity = reorientedInput * _settings.MaxStableMoveSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-_settings.StableMovementSharpness * deltaTime));
        }

        private void UpdateAirMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                var targetVelocity = _moveInputVector * _settings.MaxAirMoveSpeed;

                if (_motor.GroundingStatus.FoundAnyGround)
                {
                    var obstructionNormal = Vector3.Cross(
                        Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal), _motor.CharacterUp).normalized;

                    targetVelocity = Vector3.ProjectOnPlane(targetVelocity, obstructionNormal);
                }

                var velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, _settings.Gravity);
                currentVelocity += velocityDiff * _settings.AirAccelerationSpeed * deltaTime;
            }

            currentVelocity += _settings.Gravity * deltaTime;
            currentVelocity *= 1f / (1f + _settings.Drag * deltaTime);
        }

        private void TryJump(ref Vector3 currentVelocity)
        {
            if (!_jumpRequested)
                return;

            var canDoubleJump = _settings.AllowDoubleJump && _jumpConsumed && !_doubleJumpConsumed && !_motor.GroundingStatus.IsStableOnGround;
            if (canDoubleJump)
            {
                Jump(ref currentVelocity, _motor.CharacterUp);
                _jumpRequested = false;
                _doubleJumpConsumed = true;
                _jumpedThisFrame = true;
                return;
            }

            var isGrounded = _motor.GroundingStatus.IsStableOnGround || _timeSinceLastAbleToJump <= _settings.JumpPostGroundingGraceTime;
            if (!_jumpConsumed && isGrounded)
            {
                var jumpDir = _motor.CharacterUp;

                if (_motor.GroundingStatus is { FoundAnyGround: true, IsStableOnGround: false })
                    jumpDir = _motor.GroundingStatus.GroundNormal;

                Jump(ref currentVelocity, jumpDir);
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
            }
        }

        private void Jump(ref Vector3 currentVelocity, Vector3 direction)
        {
            _motor.ForceUnground();
            currentVelocity += (direction * _settings.JumpSpeed) - Vector3.Project(currentVelocity, _motor.CharacterUp);
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            if (_jumpRequested && _timeSinceJumpRequested > _settings.JumpPreGroundingGraceTime)
            {
                _jumpRequested = false;
            }

            if (_motor.GroundingStatus.IsStableOnGround)
            {
                if (!_jumpedThisFrame)
                {
                    _doubleJumpConsumed = false;
                    _jumpConsumed = false;
                }

                _timeSinceLastAbleToJump = 0f;
            }
            else
            {
                _timeSinceLastAbleToJump += deltaTime;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        public bool IsColliderValidForCollisions(Collider coll) => true;
    }
}
