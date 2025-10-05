using KinematicCharacterController.Examples;
using UnityEngine;

namespace TalosTest.Character
{
    public class CameraController
    {
        private readonly ExampleCharacterCamera _orbitCamera;
        private readonly Transform _followPoint;
        private readonly PlayerController _character;
        private readonly PlayerInput _input;
        
        public CameraController(
            ExampleCharacterCamera orbitCamera,
            Transform followPoint,
            PlayerController character,
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
                return;
            }

            _orbitCamera.SetFollowTransform(_followPoint);
            _orbitCamera.IgnoredColliders.Clear();

            var colliders = _followPoint.root.GetComponentsInChildren<Collider>();
            _orbitCamera.IgnoredColliders.AddRange(colliders);
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

            _character.CameraRotation = _orbitCamera.Transform.rotation;
        }

        private void ToggleCameraZoom()
        {
            var isZoomedIn = Mathf.Approximately(_orbitCamera.TargetDistance, 0f);
            _orbitCamera.TargetDistance = isZoomedIn ? _orbitCamera.DefaultDistance : 0f;
        }
    }
}
