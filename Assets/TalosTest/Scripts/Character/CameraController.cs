using KinematicCharacterController.Examples;
using UnityEngine;

namespace TalosTest.Character
{
    public class CameraController
    {
        private readonly ExampleCharacterCamera _orbitCamera;
        private readonly Transform _followPoint;
        private readonly TalosCharacterController _character;
        private readonly PlayerInput _input;

        public Quaternion CameraRotation => _orbitCamera != null 
            ? _orbitCamera.Transform.rotation 
            : Quaternion.identity;

        public CameraController(
            ExampleCharacterCamera orbitCamera,
            Transform followPoint,
            TalosCharacterController character,
            PlayerInput input)
        {
            _orbitCamera = orbitCamera;
            _followPoint = followPoint;
            _character = character;
            _input = input;

            InitializeCamera();
        }

        private void InitializeCamera()
        {
            if (_orbitCamera == null || _followPoint == null || _character == null)
            {
                Debug.LogError("[TalosCameraController] Missing dependencies.");
                return;
            }

            _orbitCamera.SetFollowTransform(_followPoint);
            _orbitCamera.IgnoredColliders.Clear();
            _orbitCamera.IgnoredColliders.AddRange(_character.GetComponentsInChildren<Collider>());
        }

        public void LateUpdate()
        {
            if (_orbitCamera == null || _input == null)
            {
                return;
            }

            var look = _input.Look;
            var zoom = _input.Zoom;

            _orbitCamera.UpdateWithInput(Time.deltaTime, zoom, new Vector3(look.x, look.y, 0f));

            if (_input.ToggleZoom)
            {
                ToggleCameraZoom();
            }
        }

        private void ToggleCameraZoom()
        {
            var isZoomedIn = Mathf.Approximately(_orbitCamera.TargetDistance, 0f);
            _orbitCamera.TargetDistance = isZoomedIn ? _orbitCamera.DefaultDistance : 0f;
        }
    }
}
