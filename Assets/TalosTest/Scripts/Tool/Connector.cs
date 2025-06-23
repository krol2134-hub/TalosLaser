using System.Collections.Generic;
using TalosTest.Character;
using TalosTest.Visuals;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Connector : MovableTool
    {
        [SerializeField] private LaserPathEffect laserPathEffectPrefab;
        
        private readonly Dictionary<LaserInteractable, LaserPathEffect> _spawnedLaserPathEffects = new();
        
        public override void PickUp(Interactor interactor)
        {
            base.PickUp(interactor);
            
            ClearConnections();
            DespawnLasers();
        }

        public override void Drop(Interactor interactor)
        {
            base.Drop(interactor);

            foreach (var laserInteractable in interactor.HeldConnections)
            {
                AddOutputConnection(laserInteractable);
                DisplayLaserPath(laserInteractable);
            }
        }

        private void DisplayLaserPath(LaserInteractable targetInteractable)
        {
            var targetLaserPosition = targetInteractable.LaserPoint;
            var laserDirection = (targetLaserPosition - LaserPoint);
                
            var laserPathEffect = Instantiate(laserPathEffectPrefab, LaserPoint, Quaternion.identity);
            laserPathEffect.SetPath(laserDirection.normalized, laserDirection.magnitude);
                
            _spawnedLaserPathEffects.Add(targetInteractable, laserPathEffect);
        }

        private void DespawnLasers()
        {
            foreach (var laserPathEffect in _spawnedLaserPathEffects.Values)
            {
                //TODO Use Pool
                Destroy(laserPathEffect.gameObject);
            }
            
            _spawnedLaserPathEffects.Clear();
        }

        protected override void RemoveOutputConnection(LaserInteractable inputInteractable)
        {
            base.RemoveOutputConnection(inputInteractable);

            if (_spawnedLaserPathEffects.TryGetValue(inputInteractable, out var laserPathEffect))
            {
                Destroy(laserPathEffect.gameObject);
                _spawnedLaserPathEffects.Remove(inputInteractable);
            }
        }

        public override bool CanConnectLaser()
        {
            return true;
        }

        public override string GetPickUpText()
        {
            return "Take Connector";
        }

        public override string GetInteractWithToolInHandsText()
        {
            return "Drop";
        }
    }
}