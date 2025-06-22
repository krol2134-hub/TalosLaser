using UnityEngine;

namespace TalosTest.Tool
{
    public class Receiver : MonoBehaviour, IConnectionPoint
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