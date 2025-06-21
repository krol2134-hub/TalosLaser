using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public class ToolGenerator : MonoBehaviour, IGenerator
    {
        public string GetConnectText()
        {
            return "Connect";
        }

        public void Connect(Interactor interactor)
        {
            throw new System.NotImplementedException();
        }
    }
}