using KinematicCharacterController;
using KinematicCharacterController.Examples;
using UnityEngine;

namespace TalosTest.Character
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class PlayerComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExampleCharacterCamera orbitCamera;
        [SerializeField] private Transform cameraFollowPoint;
        [SerializeField] private Interactor interactor;
        [SerializeField] private PlayerSettings settings;

        private PlayerInput _input;
        private CameraController _cameraController;
        private PlayerController _playerController;
        private KinematicCharacterMotor _motor;

        private void Awake()
        {
            _motor = GetComponent<KinematicCharacterMotor>();
            
            _input = new PlayerInput();
            _playerController = new PlayerController(_motor, interactor, _input, settings);
            _cameraController = new CameraController(orbitCamera, cameraFollowPoint, _playerController, _input);
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