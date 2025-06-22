using UnityEngine;

namespace TalosTest.Tool
{
    public class Receiver : LaserInteractable
    {
        [SerializeField] private Transform laserPoint;

        public override bool CanConnectLaser()
        {
            return true;
        }
    }
}