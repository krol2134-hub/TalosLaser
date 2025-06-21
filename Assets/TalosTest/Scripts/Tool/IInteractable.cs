using TalosTest.Character;

namespace TalosTest.Tool
{
    public interface IInteractable
    {
        string GetInteractText(Interactor interactor);
        void Interact(Interactor interactor);
    }
}