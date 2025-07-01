using UnityEngine;

namespace TalosTest.Tool
{
    public class Generator : LaserInteractable
    {
        [SerializeField] private ColorType color;
        
        public ColorType Color => color;

        public override bool CanConnectColor(ColorType colorType)
        {
            return false;
        }
    }
}