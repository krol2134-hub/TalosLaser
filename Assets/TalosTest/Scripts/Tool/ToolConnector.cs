using TalosTest.Character;

namespace TalosTest.Tool
{
    public class ToolConnector : MovableTool
    {
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