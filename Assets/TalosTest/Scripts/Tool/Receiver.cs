using System;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Receiver : LaserInteractable
    {
        [SerializeField] private Color targetColor = Color.red;
        [SerializeField] private GameObject activateEffect;
        
        public bool IsActivate { get; private set; }

        private void Awake()
        {
            activateEffect.SetActive(IsActivate);
        }

        public override bool CanConnectLaser()
        {
            return true;
        }

        public override void AddInputLaser(Color color)
        {
            base.AddInputLaser(color);

            UpdateActivationState();
        }
        
        private void UpdateActivationState()
        {
            var isActivate = InputLaserColors.Contains(targetColor);
            if (IsActivate != isActivate)
            {
                IsActivate = isActivate;

                activateEffect.SetActive(isActivate);
            }
        }
    }
}