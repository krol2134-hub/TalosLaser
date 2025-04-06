namespace TalosTest
{
    public class ToolConnector : MovableTool
    {

        public override void Interact(Interactor interactor)
        {
            PickUp(interactor);
        }

        public override void InteractWithToolInHands(Interactor interactor)
        {
            Place(interactor);
        }

        public override string GetInteractText(Interactor interactor)
        {
            return "Take Connector";
        }

        public override string GetInteractWithToolInHandsText(Interactor interactor)
        {
            return "Drop";
        }

    }
}