using UnityEngine;

namespace TalosTest.Tool
{
    public class ToolReceiver : MonoBehaviour, IConnectionPoint
    {
        [SerializeField] private Transform laserPoint;
        
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