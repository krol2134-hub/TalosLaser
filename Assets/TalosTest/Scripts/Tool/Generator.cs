using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Generator : LaserInteractable, IGenerator
    {
        [SerializeField] private Transform laserPoint;
        [SerializeField] private Color laserColor;
        
        public Color LaserColor => laserColor;

        public override bool CanConnectLaser()
        {
            return false;
        }

        public void Connect(Interactor interactor)
        {
            throw new System.NotImplementedException();
        }

        public string GetConnectText()
        {
            return "Connect";
        }
    }
}