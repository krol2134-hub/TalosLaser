using System;
using UnityEngine;
using System.Collections.Generic;

namespace TalosTest.Tool
{
    [SelectionBase]
    public abstract class LaserInteractable : MonoBehaviour
    {
        [SerializeField] private Transform laserPoint;
        [SerializeField] private InteractableType type;
        [SerializeField] private Outline outline;
        
        private readonly List<LaserInteractable> _outputConnections = new();
        private readonly List<LaserInteractable> _inputConnections = new();
        protected readonly List<ColorType> _inputLaserColors = new();
        
        public IReadOnlyList<LaserInteractable> OutputConnections => _outputConnections;
        public IReadOnlyList<LaserInteractable> InputConnections => _inputConnections;
        public IReadOnlyList<ColorType> InputLaserColors => _inputLaserColors;
        public Vector3 LaserPoint => laserPoint.position;
        public Transform LaserTransform => laserPoint;
        public InteractableType Type => type;

        private void Awake()
        {
            UpdateSelectVisual(false);
        }

        public virtual void Reset()
        {
            _inputLaserColors.Clear();
        }

        public virtual void AddInputColor(ColorType color)
        {
            if (!_inputLaserColors.Contains(color))
            {
                _inputLaserColors.Add(color);
            }
        }
        
        public bool CanShareLaser()
        {
            var colorsCount = _inputLaserColors.Count;
            if (_inputLaserColors.Count <= 0 || colorsCount > 1)
            {
                return false;
            }
            
            return true;
        }
        
        public abstract bool CanConnectColor(ColorType colorType);

        protected void ClearConnections()
        {
            foreach (var target in _outputConnections)
            {
                target.RemoveInputConnection(this);
            }

            for (var index = _inputConnections.Count - 1; index >= 0; index--)
            {
                var inputSource = _inputConnections[index];
                inputSource.RemoveOutputConnection(this);
            }

            _outputConnections.Clear();
            _inputConnections.Clear();
        }

        protected void AddOutputConnection(LaserInteractable target)
        {
            if (_outputConnections.Contains(target))
            {
                return;
            }
            
            _outputConnections.Add(target);
            target.AddInputSource(this);
        }

        protected virtual void RemoveOutputConnection(LaserInteractable target)
        {
            if (!_outputConnections.Contains(target))
            {
                return;
            }
            
            _outputConnections.Remove(target);
            target.RemoveInputConnection(this);
        }

        private void AddInputSource(LaserInteractable source)
        {
            if (!_inputConnections.Contains(source))
            {
                _inputConnections.Add(source);
            }
        }

        protected virtual void RemoveInputConnection(LaserInteractable inputInteractable)
        {
            _inputConnections.Remove(inputInteractable);
        }

        public string GetSelectText()
        {
            return "Select";
        }

        public void UpdateSelectVisual(bool state)
        {
            outline.enabled = state;
        }
    }
}