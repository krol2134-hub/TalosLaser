using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public class ToolGenerator : MonoBehaviour, IGenerator, IConnectionPoint
    {
        [SerializeField] private Transform laserPoint;
        
        public string GetConnectText()
        {
            return "Connect";
        }

        public void Connect(Interactor interactor)
        {
            throw new System.NotImplementedException();
        }

        public string GetSelectText()
        {
            return "Select";
        }

        public Transform GetPoint()
        {
            return laserPoint;
        }
    }
}