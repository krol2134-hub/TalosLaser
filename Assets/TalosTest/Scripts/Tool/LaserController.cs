using System.Collections.Generic;
using TalosTest.Visuals;
using UnityEngine;

namespace TalosTest.Tool
{
    //TODO Need optimize: Replace FindObjectsOfType, Use event for process lasers instead Update
    public class LaserController : MonoBehaviour
    {
        private readonly HashSet<LaserInteractable> _checkedInteractables = new();
        private readonly Queue<(LaserInteractable, Color)> _checkQueue = new();

        private readonly Dictionary<Generator, Dictionary<LaserInteractable, LaserEffect>> _lasersByGenerators = new();
        private readonly List<LaserEffect> _useLasers = new();
        private readonly List<LaserInteractable> _unusedLaserInteractable = new();
        
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

        private void Start()
        {
            foreach (var generator in _generators)
            {
                _lasersByGenerators.TryAdd(generator, new Dictionary<LaserInteractable, LaserEffect>());
            }
        }

        private void Update()
        {
            ResetAll();
            GeneratorLasersUpdate();
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

        private void GeneratorLasersUpdate()
        {
            foreach (var generator in _generators)
            {
                _useLasers.Clear();
                _unusedLaserInteractable.Clear();
                
                var lasersByInteractables = _lasersByGenerators[generator];
                foreach (var source in generator.InputConnections)
                {
                    if (!lasersByInteractables.TryGetValue(source, out var laser))
                    {
                        laser = CreateLaserEffect(generator, source);

                        lasersByInteractables.Add(source, laser);
                    }

                    _useLasers.Add(laser);
                    laser.Clear();
                    laser.AddPoint(source.LaserPoint);

                    LaserProcess(source, laser, generator.LaserColor);
                }

                ClearLaserEffects(lasersByInteractables);
            }
        }

        private static LaserEffect CreateLaserEffect(Generator generator, LaserInteractable source)
        {
            var laser = Instantiate(generator.LaserEffectEffectPrefab, generator.LaserPoint, Quaternion.identity,
                generator.LaserTransform);
            laser.transform.LookAt(source.LaserPoint);
            return laser;
        }

        private void ClearLaserEffects(Dictionary<LaserInteractable, LaserEffect> lasersByInteractables)
        {
            foreach (var (interactable, laser) in lasersByInteractables)
            {
                if (!_useLasers.Contains(laser))
                {
                    _unusedLaserInteractable.Add(interactable);
                }
            }

            foreach (var laserInteractable in _unusedLaserInteractable)
            {
                if (lasersByInteractables.Remove(laserInteractable, out var laser ))
                {
                    Destroy(laser.gameObject);
                }
            }
        }

        private void LaserProcess(LaserInteractable startInteractable, LaserEffect laserEffect, Color color)
        {
            _checkedInteractables.Clear();
            _checkQueue.Clear();
            
            _checkQueue.Enqueue((startInteractable, color));
            _checkedInteractables.Add(startInteractable);

            while (_checkQueue.Count > 0)
            {
                var (current, currentColor) = _checkQueue.Dequeue();
                
                var targets = current.OutputConnections;
                UpdateInteractables(laserEffect, currentColor, targets);

                var sources = current.InputConnections;
                UpdateInteractables(laserEffect, currentColor, sources);
            }
        }

        private void UpdateInteractables(LaserEffect laserEffect, Color currentColor, IReadOnlyList<LaserInteractable> interactables)
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
                
                laserEffect.AddPoint(target.LaserPoint);
                target.AddInputLaser(currentColor);
                
                _checkedInteractables.Add(target);
                _checkQueue.Enqueue((target, currentColor));
            }
        }
    }
}