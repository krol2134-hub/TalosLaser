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
}