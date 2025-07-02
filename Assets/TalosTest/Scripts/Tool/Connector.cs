using System;
using System.Collections.Generic;
using TalosTest.Character;
using TalosTest.Visuals;
using UnityEditor;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Connector : MovableTool
    {
        [SerializeField] private LaserPathEffect laserPathEffectPrefab;
        
        private readonly Dictionary<LaserInteractable, LaserPathEffect> _spawnedLaserPathEffects = new();

        public Action<Connector> OnPickUp;
        public Action<Connector> OnDrop;

#if UNITY_EDITOR
        private static int _debugCounter;
        private int _debugIndex;

        private void Awake()
        {
            _debugCounter++;
            _debugIndex = _debugCounter;

            name = "Connector-" + _debugIndex;
        }
#endif
        
        public override void PickUp(Interactor interactor)
        {
            base.PickUp(interactor);
            
            ClearConnections();
            DespawnLasers();

            OnPickUp?.Invoke(this);
        }

        public override void Drop(Interactor interactor)
        {
            base.Drop(interactor);

            foreach (var laserInteractable in interactor.HeldConnections)
            {
                AddOutputConnection(laserInteractable);
                DisplayLaserPath(laserInteractable);
            }
            
            OnDrop?.Invoke(this);
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

        public override bool CanConnectColor(ColorType colorType)
        {
            return true;
        }

        public override string GetPickUpText()
        {
            return "Take Connector";
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.BeginGUI();
            
            var style = new GUIStyle
            {
                fontSize = 25,
                normal =
                {
                    textColor = Color.green
                }
            };
            Handles.Label(LaserPoint + Vector3.up * 0.55f, _debugIndex.ToString(), style);
            
            Handles.EndGUI();
        }  
#endif
    }
}