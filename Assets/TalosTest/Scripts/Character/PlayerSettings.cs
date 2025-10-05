using UnityEngine;

namespace TalosTest.Character
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "TalosTest/Player Settings", order = 0)]
    public class PlayerSettings : ScriptableObject
    {
        [Header("Stable Movement")]
        [SerializeField] private float maxStableMoveSpeed = 10f;
        [SerializeField] private float stableMovementSharpness = 15f;
        [SerializeField] private float orientationSharpness = 10f;

        [Header("Air Movement")]
        [SerializeField] private float maxAirMoveSpeed = 10f;
        [SerializeField] private float airAccelerationSpeed = 5f;
        [SerializeField] private float drag = 0.1f;

        [Header("Jumping")]
        [SerializeField] private bool allowDoubleJump = true;
        [SerializeField] private float jumpSpeed = 10f;
        [SerializeField] private float jumpPreGroundingGraceTime = 0.15f;
        [SerializeField] private float jumpPostGroundingGraceTime = 0.2f;

        [Header("Gravity")]
        [SerializeField] private Vector3 gravity = new(0f, -30f, 0f);

        public float MaxStableMoveSpeed => maxStableMoveSpeed;
        public float StableMovementSharpness => stableMovementSharpness;
        public float OrientationSharpness => orientationSharpness;

        public float MaxAirMoveSpeed => maxAirMoveSpeed;
        public float AirAccelerationSpeed => airAccelerationSpeed;
        public float Drag => drag;

        public bool AllowDoubleJump => allowDoubleJump;
        public float JumpSpeed => jumpSpeed;
        public float JumpPreGroundingGraceTime => jumpPreGroundingGraceTime;
        public float JumpPostGroundingGraceTime => jumpPostGroundingGraceTime;

        public Vector3 Gravity => gravity;
    }
}