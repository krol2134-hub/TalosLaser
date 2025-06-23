using System;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Receiver : LaserInteractable
    {
        [SerializeField] private ColorType targetColor;
        [SerializeField] private GameObject activateEffect;
        
        public bool IsActivate { get; private set; }

        public event Action OnStateChanged;

        private void Awake()
        {
            activateEffect.SetActive(IsActivate);
        }

        public override bool CanConnectLaser()
        {
            return true;
        }

        public override void AddInputLaser(ColorType color)
        {
            base.AddInputLaser(color);

            UpdateActivationState();
        }
        
        private void UpdateActivationState()
        {
            var isActivate = InputLaserColors.Contains(targetColor);

            var isSameState = IsActivate == isActivate;
            if (isSameState)
            {
                return;
            }
            
            IsActivate = isActivate;

            activateEffect.SetActive(isActivate);
                
            OnStateChanged?.Invoke();
        }
    }
}