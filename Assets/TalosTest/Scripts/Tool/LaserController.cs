using System.Collections.Generic;
using TalosTest.Visuals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TalosTest.Tool
{
    //TODO Need optimize: Replace FindObjectsOfType, Use event for process lasers instead Update
    public class LaserController : MonoBehaviour
    {
        [SerializeField] private LaserEffectPool laserEffectPool;
        [SerializeField] private float gizmosTime = 0f;
        
        private readonly HashSet<LaserInteractable> _checkedInteractablesBuffer = new();
        private readonly Queue<(LaserInteractable, ColorType)> _checkQueue = new();
        private readonly Dictionary<Generator, List<LaserInteractable>> _alreadyCheckedGeneratorInputs = new();
        private readonly Dictionary<LaserInteractable, LaserInteractable> _pathCameFromBuffer = new();
        private readonly List<LaserInteractable> _pathBuffer = new();
        
        private readonly Dictionary<(Vector3 start, Vector3 end), LaserEffect> _activeLaserEffects = new();
        private readonly List<(Vector3 start, Vector3 end)> _usedLasersBuffer = new();
        private readonly List<(Vector3 start, Vector3 end)> _removeLasersBuffer = new();
        
        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private bool _isMidBlocker;

        private Vector3 LaserDebugOffset => new(Random.Range(0f, 0.2f), 0, Random.Range(0f, 0.2f));

        private void Awake()
        {
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();
            
            foreach (var generator in _generators)
            {
                _alreadyCheckedGeneratorInputs.Add(generator, new List<LaserInteractable>());
            }
        }

        private void OnEnable()
        {
            foreach (var connector in _connectors)
            {
                connector.OnPickUp += OnPickUp;
                connector.OnDrop += OnPickUp;
            }
        }

        private void OnDisable()
        {
            foreach (var connector in _connectors)
            {
                connector.OnPickUp -= OnPickUp;
                connector.OnDrop -= OnPickUp;
            }
        }

        private void Update()
        {
            UpdateGeneratorLasers();
        }

        private void OnPickUp(Connector connector)
        {
            UpdateGeneratorLasers();
        }

        private void UpdateGeneratorLasers()
        {
            ResetAll();
            _usedLasersBuffer.Clear();
            
            foreach (var checkedGeneratorInputs in _alreadyCheckedGeneratorInputs.Values)
            {
                checkedGeneratorInputs.Clear();
            }
            
            foreach (var generator in _generators)
            {
                UpdateGeneratorLaserInput(generator);
            }

            ClearLaserEffects();
        }

        private void ResetAll()
        {
            ResetInteractables(_generators);
            ResetInteractables(_receivers);
            ResetInteractables(_connectors);
        }

        private void UpdateGeneratorLaserInput(Generator generator)
        {
            foreach (var sourceInput in generator.InputConnections)
            {
                var inputConnectionsToSkip = _alreadyCheckedGeneratorInputs[generator];
                var isAlreadyChecked = inputConnectionsToSkip.Contains(sourceInput);
                if (isAlreadyChecked)
                {
                    continue;
                }
                    
                _checkedInteractablesBuffer.Clear();
                _checkQueue.Clear();

                Generator conflictGenerator = default;
                LaserInteractable conflictInput = default;
                CheckConflictGeneratorInPath(generator, sourceInput, ref conflictGenerator, ref conflictInput);

                //TODO Refactor for use many conflicts generators
                if (conflictGenerator != null)
                {
                    GenerateConflictLaser(generator, conflictGenerator, sourceInput, conflictInput);
                }
                else
                {
                    GenerateLaser(generator, sourceInput, null);
                }
            }
        }

        private void CheckConflictGeneratorInPath(Generator startGenerator, LaserInteractable startInteractable, 
            ref Generator conflictGenerator, ref LaserInteractable conflictInput)
        {
            _checkQueue.Enqueue((startInteractable, default));
            _checkedInteractablesBuffer.Add(startInteractable);
            
            while (_checkQueue.Count > 0)
            {
                var (current, _) = _checkQueue.Dequeue();
                
                var targets = current.OutputConnections;
                CheckConflictGeneratorInConnection(targets, startGenerator, current, ref conflictGenerator, ref conflictInput);

                var sources = current.InputConnections;
                CheckConflictGeneratorInConnection(sources, startGenerator, current, ref conflictGenerator, ref conflictInput);
            }
            
            _checkQueue.Clear();
        }

        private void CheckConflictGeneratorInConnection(IReadOnlyList<LaserInteractable> connections, Generator startGenerator,
            LaserInteractable previous, ref Generator conflictGenerator, ref LaserInteractable conflictInput)
        {
            foreach (var target in connections)
            {
                var isChecked = _checkedInteractablesBuffer.Contains(target);
                if (isChecked)
                {
                    continue;
                }

                var isAcceptRay = target.CanConnectLaser();
                if (!isAcceptRay)
                {
                    if (target != startGenerator && target is Generator generator && startGenerator.LaserColor != generator.LaserColor)
                    {
                        conflictGenerator = generator;
                        conflictInput = previous;
                    }
                    
                    continue;
                }
                
                _checkedInteractablesBuffer.Add(target);
                _checkQueue.Enqueue((target, default));
            }
        }

        private void GenerateConflictLaser(Generator generator, Generator conflictGenerator, LaserInteractable sourceInput, LaserInteractable conflictInput)
        {
            if (_alreadyCheckedGeneratorInputs.TryGetValue(conflictGenerator, out var inputsToSkip))
            {
                inputsToSkip.Add(conflictInput);
            }
            
            var pathInteractables = FindShortestPathBetweenGenerators(generator, conflictGenerator);
            if (pathInteractables == null || pathInteractables.Count == 0)
            {
                return;
            }
                
            _isMidBlocker = pathInteractables.Count % 2 != 0;
            if (_isMidBlocker)
            {
                var index = pathInteractables.Count / 2;
                var midConnector = pathInteractables[index];

                GenerateLaser(generator, sourceInput, midConnector);
                GenerateLaser(conflictGenerator, conflictInput, midConnector);
                
                Debug.DrawLine(midConnector.LaserPoint, midConnector.LaserPoint + Vector3.up * 10f, Color.black, gizmosTime);
            }
            else
            {
                var firstIndex = pathInteractables.Count / 2 - 1;
                var secondIndex = firstIndex + 1;
                var firstBlockConnector = pathInteractables[secondIndex];
                var secondBlockConnector = pathInteractables[firstIndex];

                GenerateLaser(generator, sourceInput, firstBlockConnector);
                GenerateLaser(conflictGenerator, conflictInput, secondBlockConnector);
                
                Debug.DrawLine(firstBlockConnector.LaserPoint, firstBlockConnector.LaserPoint + Vector3.up * 10f, Color.black, gizmosTime);
                Debug.DrawLine(secondBlockConnector.LaserPoint, secondBlockConnector.LaserPoint + Vector3.up * 10f, Color.black, gizmosTime);
            }
        }

        private List<LaserInteractable> FindShortestPathBetweenGenerators(LaserInteractable start, LaserInteractable target)
        {
            _pathBuffer.Clear();
            _checkedInteractablesBuffer.Clear();
            _checkQueue.Clear();
            _pathCameFromBuffer.Clear();
            
            _checkQueue.Enqueue((start, default));
            _checkedInteractablesBuffer.Add(start);

            while (_checkQueue.Count > 0)
            {
                var (current, _) = _checkQueue.Dequeue();

                if (current == target)
                {
                    var node = target;
                    while (node != null)
                    {
                        _pathBuffer.Add(node);
                        _pathCameFromBuffer.TryGetValue(node, out node);
                    }
                    
                    _pathBuffer.Reverse();
                    return _pathBuffer;
                }

                GetPathConnections(current, current.InputConnections);
                GetPathConnections(current, current.OutputConnections);
            }

            return null;
        }

        private void GetPathConnections(LaserInteractable current, IReadOnlyList<LaserInteractable> connections)
        {
            foreach (var connection in connections)
            {
                if (_checkedInteractablesBuffer.Add(connection))
                {
                    _pathCameFromBuffer[connection] = current;
                    _checkQueue.Enqueue((connection, default));
                }
            }
        }

        private void GenerateLaser(Generator generator, LaserInteractable source, LaserInteractable blockerInteractable)
        {
            _checkedInteractablesBuffer.Clear();
            
            DrawLaserEffect(generator.LaserColor, generator.LaserPoint, source.LaserPoint);
            Debug.DrawLine(generator.LaserPoint, source.LaserPoint + LaserDebugOffset, generator.LaserColor == ColorType.Blue ? Color.blue :  Color.red, gizmosTime);

            LaserProcess(source, generator.LaserColor, blockerInteractable);
        }


        private void ResetInteractables(IReadOnlyCollection<LaserInteractable> laserInteractables)
        {
            foreach (var laserInteractable in laserInteractables)
            {
                laserInteractable.Reset();
            }
        }

        private void LaserProcess(LaserInteractable startInteractable, ColorType color, LaserInteractable blockerInteractable)
        {
            _checkedInteractablesBuffer.Clear();

            _checkQueue.Enqueue((startInteractable, color));
            _checkedInteractablesBuffer.Add(startInteractable);

            while (_checkQueue.Count > 0)
            {
                var (current, currentColor) = _checkQueue.Dequeue();
                
                if (blockerInteractable != null && blockerInteractable == current)
                {
                    continue;
                }
                
                var outputConnections = current.OutputConnections;
                UpdateLaserConnections(current, outputConnections, currentColor, blockerInteractable);

                var inputConnections = current.InputConnections;
                UpdateLaserConnections(current, inputConnections, currentColor, blockerInteractable);
            }
        }

        private void UpdateLaserConnections(LaserInteractable current, IReadOnlyList<LaserInteractable> connections, ColorType currentColor, LaserInteractable blockerInteractable)
        {
            foreach (var target in connections)
            {
                var isChecked = _checkedInteractablesBuffer.Contains(target);
                if (isChecked)
                {
                    continue;
                }

                var isAcceptRay = target.CanConnectLaser();
                if (!isAcceptRay)
                {
                    continue;
                }
                
                if (blockerInteractable != null && blockerInteractable == target)
                {
                    if (_isMidBlocker)
                    {
                        DrawLaserEffect(currentColor, current.LaserPoint, target.LaserPoint);
                        Debug.DrawLine(current.LaserPoint, target.LaserPoint + LaserDebugOffset, currentColor == ColorType.Blue ? Color.blue : Color.red, gizmosTime);
                    }
                    else
                    {
                        var direction = target.LaserPoint - current.LaserPoint;
                        var distance = direction.magnitude / 2;
                        var targetPoint = current.LaserPoint + direction.normalized * distance;

                        DrawLaserEffect(currentColor, current.LaserPoint, targetPoint);
                        Debug.DrawRay(current.LaserPoint, direction.normalized * distance, currentColor == ColorType.Blue ? Color.blue : Color.red, gizmosTime);
                    }
                    
                    target.AddInputLaser(currentColor);
                    _checkedInteractablesBuffer.Add(target);
                    
                    continue;
                }

                target.AddInputLaser(currentColor);

                DrawLaserEffect(currentColor, current.LaserPoint, target.LaserPoint);
                Debug.DrawLine(current.LaserPoint, target.LaserPoint + LaserDebugOffset, currentColor == ColorType.Blue ? Color.blue : Color.red, gizmosTime);

                _checkedInteractablesBuffer.Add(target);
                _checkQueue.Enqueue((target, currentColor));
            }
        }

        private void DrawLaserEffect(ColorType currentColor, Vector3 startPoint, Vector3 targetPoint)
        {
            var startTargetPoint = (startPoint, targetPoint);

            if (_activeLaserEffects.TryGetValue(startTargetPoint, out var laserEffect))
            {
                UpdateLaserEffect(laserEffect, startPoint, targetPoint);
            }
            else
            {
                laserEffect = laserEffectPool.Get(currentColor, startPoint, targetPoint);
                UpdateLaserEffect(laserEffect, startPoint, targetPoint);
                _activeLaserEffects.Add(startTargetPoint, laserEffect);
            }

            _usedLasersBuffer.Add(startTargetPoint);
        }

        private void UpdateLaserEffect(LaserEffect laserEffect, Vector3 startPoint, Vector3 targetPoint)
        {
            laserEffect.transform.position = startPoint;
            laserEffect.transform.LookAt(targetPoint);
        }
        
        private void ClearLaserEffects()
        {
            _removeLasersBuffer.Clear();
            foreach (var activeLaserEffect in _activeLaserEffects)
            {
                if (!_usedLasersBuffer.Contains(activeLaserEffect.Key))
                {
                    laserEffectPool.Release(activeLaserEffect.Value);
                    _removeLasersBuffer.Add(activeLaserEffect.Key);
                }
            }

            foreach (var key in _removeLasersBuffer)
            {
                _activeLaserEffects.Remove(key);
            }
        }
    }
}