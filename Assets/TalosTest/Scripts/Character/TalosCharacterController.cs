using KinematicCharacterController;
using UnityEngine;

namespace TalosTest.Character
{
    [System.Serializable]
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool Interact;
    }

    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class TalosCharacterController : MonoBehaviour, ICharacterController
    {
        [Header("References")]
        [SerializeField] private KinematicCharacterMotor motor;
        [SerializeField] private Interactor interactor;

        [Header("Stable Movement")]
        [SerializeField] private float maxStableMoveSpeed = 10f;
        [SerializeField] private float stableMovementSharpness = 15f;
        [SerializeField] private float orientationSharpness = 10f;

        [Header("Air Movement")]
        [SerializeField] private float maxAirMoveSpeed = 10f;
        [SerializeField] private float airAccelerationSpeed = 5f;
        [SerializeField] private float drag = 0.1f;

        [Header("Jumping")]
        [SerializeField] private bool allowJumpingWhenSliding;
        [SerializeField] private bool allowDoubleJump;
        [SerializeField] private float jumpSpeed = 10f;
        [SerializeField] private float jumpPreGroundingGraceTime;
        [SerializeField] private float jumpPostGroundingGraceTime;
        [SerializeField] private Vector3 gravity = new(0f, -30f, 0f);

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpRequested;
        private bool _jumpConsumed;
        private bool _jumpedThisFrame;
        private bool _doubleJumpConsumed;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump;

        private void Start()
        {
            if (motor == null)
            {
                motor = GetComponent<KinematicCharacterMotor>();
            }

            motor.CharacterController = this;
        }

        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            var moveInput = new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward);
            _moveInputVector = Vector3.ClampMagnitude(moveInput, 1f);

            var cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, motor.CharacterUp).normalized;
            }

            var cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);
            _moveInputVector = cameraPlanarRotation * _moveInputVector;
            _lookInputVector = cameraPlanarDirection;

            if (inputs.JumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }

            if (inputs.Interact && interactor != null)
            {
                interactor.HandleInteractInput();
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector == Vector3.zero || orientationSharpness <= 0f)
            {
                return;
            }

            var progress = 1f - Mathf.Exp(-orientationSharpness * deltaTime);
            var smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, progress).normalized;

            currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;

            if (motor.GroundingStatus.IsStableOnGround)
            {
                HandleGroundMovement(ref currentVelocity, deltaTime);
            }
            else
            {
                HandleAirMovement(ref currentVelocity, deltaTime);
            }

            HandleJumping(ref currentVelocity);
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            HandleJumpTimers(deltaTime);
        }

        private void HandleGroundMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            var inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
            var reorientedInput = Vector3.Cross(motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            var targetVelocity = reorientedInput * maxStableMoveSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-stableMovementSharpness * deltaTime));
        }

        private void HandleAirMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                var targetVelocity = _moveInputVector * maxAirMoveSpeed;

                if (motor.GroundingStatus.FoundAnyGround)
                {
                    var obstructionNormal = Vector3.Cross(
                        Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal),
                        motor.CharacterUp
                    ).normalized;

                    targetVelocity = Vector3.ProjectOnPlane(targetVelocity, obstructionNormal);
                }

                var velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, gravity);
                currentVelocity += velocityDiff * airAccelerationSpeed * deltaTime;
            }

            currentVelocity += gravity * deltaTime;
            currentVelocity *= 1f / (1f + drag * deltaTime);
        }

        private void HandleJumping(ref Vector3 currentVelocity)
        {
            if (!_jumpRequested)
            {
                return;
            }

            var canDoubleJump = allowDoubleJump &&
                                _jumpConsumed &&
                                !_doubleJumpConsumed &&
                                (allowJumpingWhenSliding
                                    ? !motor.GroundingStatus.FoundAnyGround
                                    : !motor.GroundingStatus.IsStableOnGround);

            if (canDoubleJump)
            {
                PerformJump(ref currentVelocity, motor.CharacterUp);
                _jumpRequested = false;
                _doubleJumpConsumed = true;
                _jumpedThisFrame = true;
                return;
            }

            if (!_jumpConsumed &&
                ((allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround)
                 || _timeSinceLastAbleToJump <= jumpPostGroundingGraceTime))
            {
                var jumpDir = motor.CharacterUp;
                if (motor.GroundingStatus is { FoundAnyGround: true, IsStableOnGround: false })
                {
                    jumpDir = motor.GroundingStatus.GroundNormal;
                }

                PerformJump(ref currentVelocity, jumpDir);
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
            }
        }

        private void PerformJump(ref Vector3 currentVelocity, Vector3 direction)
        {
            motor.ForceUnground();
            currentVelocity += (direction * jumpSpeed) - Vector3.Project(currentVelocity, motor.CharacterUp);
        }

        private void HandleJumpTimers(float deltaTime)
        {
            if (_jumpRequested && _timeSinceJumpRequested > jumpPreGroundingGraceTime)
            {
                _jumpRequested = false;
            }

            if (allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround)
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

        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    }
}