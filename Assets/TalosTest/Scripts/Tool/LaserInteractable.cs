using UnityEngine;
using System.Collections.Generic;

namespace TalosTest.Tool
{
    public abstract class LaserInteractable : MonoBehaviour
    {
        [SerializeField] private Transform laserPoint;
        
        private readonly List<LaserInteractable> _outputConnections = new();
        private readonly List<LaserInteractable> _inputConnections = new();
        protected readonly List<ColorType> InputLaserColors = new();
        
        public IReadOnlyList<LaserInteractable> OutputConnections => _outputConnections.AsReadOnly();
        public IReadOnlyList<LaserInteractable> InputConnections => _inputConnections.AsReadOnly();
        public Vector3 LaserPoint => laserPoint.position;
        public Transform LaserTransform => laserPoint;

        public virtual void Reset()
        {
            InputLaserColors.Clear();
        }

        public virtual void AddInputLaser(ColorType color)
        {
            InputLaserColors.Add(color);
        }
        
        public abstract bool CanConnectLaser();

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
    }
}