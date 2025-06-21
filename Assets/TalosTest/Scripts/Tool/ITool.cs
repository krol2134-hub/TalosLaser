using TalosTest.Character;

namespace TalosTest.Tool
{
    public interface ITool
    {
        public string GetPickUpText();
        public void PickUp(Interactor interactor);
        public void Drop(Interactor interactor);

    }
}