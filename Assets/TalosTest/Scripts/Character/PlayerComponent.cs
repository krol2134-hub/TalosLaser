using KinematicCharacterController.Examples;
using UnityEngine;

namespace TalosTest.Character
{
    public class PlayerComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TalosCharacterController character;
        [SerializeField] private ExampleCharacterCamera orbitCamera;
        [SerializeField] private Transform cameraFollowPoint;

        private PlayerInput _input;
        private CameraController _cameraController;
        private PlayerController _playerController;

        private void Awake()
        {
            if (character == null)
            {
                character = GetComponent<TalosCharacterController>();
            }

            _input = new PlayerInput();
            _cameraController = new CameraController(orbitCamera, cameraFollowPoint, character, _input);
            _playerController = new PlayerController(character, _cameraController, _input);
        }

        private void Update()
        {
            _input.Update();
            _playerController.Update();
        }

        private void LateUpdate()
        {
            _cameraController.LateUpdate();
        }
    }
}