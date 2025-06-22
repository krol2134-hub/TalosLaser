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

        private void Awake()
        {
            //TODO Need optimize
            _generators = FindObjectsOfType<Generator>();
        }

        private void Update()
        {
            foreach (var generator in _generators)
            {
                foreach (var source in generator.InputSources)
                {
                    Debug.DrawLine(generator.LaserPoint, source.LaserPoint, generator.LaserColor);
                    LaserProcess(source, generator.LaserColor);
                }
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
                
                var targets = current.ConnectedTargets;
                UpdateInteractables(current, currentColor, targets);

                var sources = current.InputSources;
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
                _checkedInteractables.Add(target);
                _checkQueue.Enqueue((target, currentColor));
            }
        }
    }
}