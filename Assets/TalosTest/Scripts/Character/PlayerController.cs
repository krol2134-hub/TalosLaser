namespace TalosTest.Character
{
    public class PlayerController
    {
        private readonly TalosCharacterController _character;
        private readonly CameraController _cameraController;
        private readonly PlayerInput _input;

        public PlayerController(
            TalosCharacterController character,
            CameraController cameraController,
            PlayerInput input)
        {
            _character = character;
            _cameraController = cameraController;
            _input = input;
        }

        public void Update()
        {
            if (_character == null || _cameraController == null || _input == null)
            {
                return;
            }

            var inputs = new PlayerCharacterInputs
            {
                MoveAxisForward = _input.Move.y,
                MoveAxisRight = _input.Move.x,
                CameraRotation = _cameraController.CameraRotation,
                JumpDown = _input.Jump,
                Interact = _input.Interact
            };

            _character.SetInputs(ref inputs);
        }
    }
}