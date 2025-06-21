using TalosTest.Character;

namespace TalosTest.Tool
{
    public interface IInteractable
    {
        public string GetInteractText();
        public void Interact(Interactor interactor);
    }
}