using TalosTest.Character;
using TalosTest.Visuals;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Generator : LaserInteractable, IGenerator
    {
        [SerializeField] private Color laserColor;
        [SerializeField] private LaserEffect laserEffectEffectPrefab;
        
        public Color LaserColor => laserColor;
        public LaserEffect LaserEffectEffectPrefab => laserEffectEffectPrefab;

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