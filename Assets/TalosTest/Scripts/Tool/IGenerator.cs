using TalosTest.Character;

namespace TalosTest.Tool
{
    public interface IGenerator
    {
        public string GetConnectText();
        public void Connect(Interactor interactor);
    }
}