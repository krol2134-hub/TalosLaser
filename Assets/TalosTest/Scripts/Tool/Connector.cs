using TalosTest.Character;

namespace TalosTest.Tool
{
    public class Connector : MovableTool
    {
        public override void PickUp(Interactor interactor)
        {
            base.PickUp(interactor);
            
            ClearConnections();
        }

        public override void Drop(Interactor interactor)
        {
            base.Drop(interactor);

            foreach (var laserInteractable in interactor.HeldConnections)
            {
                AddOutputConnection(laserInteractable);
            }
        }

        public override bool CanConnectLaser()
        {
            return true;
        }

        public override string GetPickUpText()
        {
            return "Take Connector";
        }

        public override string GetInteractWithToolInHandsText()
        {
            return "Drop";
        }
    }
}