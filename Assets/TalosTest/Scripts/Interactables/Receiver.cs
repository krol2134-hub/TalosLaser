using System;
using UnityEngine;

namespace TalosTest.Interactables
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

        public override bool CanConnectColor(ColorType colorType)
        {
            return colorType == targetColor;
        }

        public override void Reset()
        {
            base.Reset();
            
            UpdateActivationState();
        }
        
        public override void AddInputColor(ColorType color)
        {
            base.AddInputColor(color);

            UpdateActivationState();
        }
        
        private void UpdateActivationState()
        {
            var isActivate = _inputLaserColors.Contains(targetColor);

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