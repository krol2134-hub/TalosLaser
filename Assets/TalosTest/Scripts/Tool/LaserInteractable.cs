using UnityEngine;
using System.Collections.Generic;

namespace TalosTest.Tool
{
    public abstract class LaserInteractable : MonoBehaviour
    {
        private readonly List<LaserInteractable> _outputConnections = new();
        private readonly List<LaserInteractable> _inputConnections = new();

        public IReadOnlyList<LaserInteractable> ConnectedTargets => _outputConnections.AsReadOnly();
        public IReadOnlyList<LaserInteractable> InputSources => _inputConnections.AsReadOnly();

        public virtual void Reset()
        {
            _inputConnections.Clear();
        }

        public abstract bool CanConnectLaser();

        protected void ClearConnections()
        {
            foreach (var target in _outputConnections)
            {
                target.RemoveInputSource(this);
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

        private void RemoveOutputConnection(LaserInteractable target)
        {
            if (!_outputConnections.Contains(target))
            {
                return;
            }
            
            _outputConnections.Remove(target);
            target.RemoveInputSource(this);
        }

        private void AddInputSource(LaserInteractable source)
        {
            if (!_inputConnections.Contains(source))
            {
                _inputConnections.Add(source);
            }
        }

        private void RemoveInputSource(LaserInteractable source)
        {
            _inputConnections.Remove(source);
        }

        public string GetSelectText()
        {
            return "Select";
        }
    }
}