using TalosTest.Character;
using TalosTest.Visuals;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Generator : LaserInteractable, IGenerator
    {
        [SerializeField] private ColorType laserColor;
        [SerializeField] private LaserEffect laserEffectEffectPrefab;
        
        public ColorType LaserColor => laserColor;
        public LaserEffect LaserEffectEffectPrefab => laserEffectEffectPrefab;

        public override bool CanConnectColor(ColorType colorType)
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