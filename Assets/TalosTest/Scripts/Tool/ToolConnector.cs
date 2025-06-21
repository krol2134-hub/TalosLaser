using TalosTest.Character;

namespace TalosTest.Tool
{
    public class ToolConnector : MovableTool
    {
        public override void Interact(Interactor interactor)
        {
            PickUp(interactor);
        }

        public override string GetInteractText()
        {
            return "Take Connector";
        }

        public override string GetInteractWithToolInHandsText()
        {
            return "Drop";
        }
    }
}