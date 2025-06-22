using System.Collections.Generic;
using UnityEngine;

namespace TalosTest.Tool
{
    //TODO Need optimize: Replace FindObjectsOfType, Use event for process lasers instead Update
    public class LaserController : MonoBehaviour
    {
        private readonly HashSet<LaserInteractable> _checkedInteractables = new();
        private readonly Queue<(LaserInteractable, Color)> _checkQueue = new();
        
        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private void Awake()
        {
            //TODO Need optimize
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();
        }

        private void Update()
        {
            ResetAll();

            foreach (var generator in _generators)
            {
                foreach (var source in generator.InputConnections)
                {
                    Debug.DrawLine(generator.LaserPoint, source.LaserPoint, generator.LaserColor);
                    LaserProcess(source, generator.LaserColor);
                }
            }
        }

        private void ResetAll()
        {
            ResetInteractables(_generators);
            ResetInteractables(_receivers);
            ResetInteractables(_connectors);
        }

        private void ResetInteractables(IReadOnlyCollection<LaserInteractable> laserInteractables)
        {
            foreach (var laserInteractable in laserInteractables)
            {
                laserInteractable.Reset();
            }
        }
        
        private void LaserProcess(LaserInteractable startInteractable, Color color)
        {
            _checkedInteractables.Clear();
            _checkQueue.Clear();
            
            _checkQueue.Enqueue((startInteractable, color));
            _checkedInteractables.Add(startInteractable);

            while (_checkQueue.Count > 0)
            {
                var (current, currentColor) = _checkQueue.Dequeue();
                
                var targets = current.OutputConnections;
                UpdateInteractables(current, currentColor, targets);

                var sources = current.InputConnections;
                UpdateInteractables(current, currentColor, sources);
            }
        }

        private void UpdateInteractables(LaserInteractable current, Color currentColor, IReadOnlyList<LaserInteractable> interactables)
        {
            foreach (var target in interactables)
            {
                var isChecked = _checkedInteractables.Contains(target);
                if (isChecked)
                {
                    continue;
                }

                var isAcceptRay = target.CanConnectLaser();
                if (!isAcceptRay)
                {
                    continue;
                }
                
                Debug.DrawLine(current.LaserPoint, target.LaserPoint, currentColor);
                target.AddInputLaser(currentColor);
                _checkedInteractables.Add(target);
                _checkQueue.Enqueue((target, currentColor));
            }
        }
    }
}