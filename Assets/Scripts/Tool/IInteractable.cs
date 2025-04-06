namespace TalosTest
{
    public interface IInteractable
    {
        string GetInteractText(Interactor interactor);
        void Interact(Interactor interactor);
    }
}