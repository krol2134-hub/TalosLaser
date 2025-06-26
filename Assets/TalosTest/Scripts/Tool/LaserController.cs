using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TalosTest.Tool
{
    //TODO Need optimize: Replace FindObjectsOfType, Use event for process lasers instead Update
    public class LaserController : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private LaserVfxController laserVFXController;
#if UNITY_EDITOR
        [SerializeField] private int pathCountDebug;
        [SerializeField, Range(-1, 10)] private int maxPathCountDebug = -1;
        [SerializeField, Range(-1, 10)] private int onlyPathTargetDebug = -1;
#endif

        private readonly Dictionary<Generator, List<LaserInteractable>> _alreadyCheckedGeneratorInputs = new();
        private readonly List<List<LaserInteractable>> _allPathsReverse = new();
        
        private readonly List<(LaserInteractable, LaserInteractable)> _conflictConnectors = new();
        
        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private LaserPathGenerator _laserPathGenerator;
        private List<LaserInteractable> _connectorBlockers = new();
        
        private void Awake()
        {
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();

            _laserPathGenerator = new LaserPathGenerator(obstacleMask);
            
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
            
            foreach (var checkedGeneratorInputs in _alreadyCheckedGeneratorInputs.Values)
            {
                checkedGeneratorInputs.Clear();
            }
            
            foreach (var generator in _generators)
            {
                foreach (var checkGenerator in _generators)
                {
                    if (generator == checkGenerator)
                    {
                        continue;
                    }
                
                    if (!TryGenerateConflictLaserPaths(generator, checkGenerator))
                    {
                        GenerateOpenLaserPath(checkGenerator);
                    }
                }
            }
        }

        private void ResetAll()
        {
            ResetInteractables(_generators);
            ResetInteractables(_receivers);
            ResetInteractables(_connectors);
            laserVFXController.Clear();
        }
        
        private bool TryGenerateConflictLaserPaths(Generator generator, Generator conflictGenerator)
        {
            var allPaths = _laserPathGenerator.FindAllPathsBetweenGenerators(generator, conflictGenerator);
            if (allPaths.Count <= 0)
            {
                return false;
            }
            
            _allPathsReverse.Clear();
            _conflictConnectors.Clear();
            
            allPaths = allPaths.OrderBy(p => p.Count).ToList();
            _allPathsReverse.AddRange(allPaths);

            var isAllProceed = IsAllGeneratorProceed(generator, allPaths);
            var isAllConflictGeneratorProceed = IsAllGeneratorProceed(conflictGenerator, allPaths);
            
            for (var index = 0; index < allPaths.Count; index++)
            {
#if UNITY_EDITOR
                if (DebugCheckPathIteration(index))
                {
                    continue;
                }
#endif
                var allPath = allPaths[index];
                for (var r = 0; r < allPath.Count; r++)
                {
                    if (r == allPath.Count - 1)
                        continue;
                    
                    var path = allPath[r];
                    var next = allPath[r + 1];
                    Debug.DrawLine(path.LaserPoint, next.LaserPoint, Color.green);
                }
            }
            
#if UNITY_EDITOR
            pathCountDebug = allPaths.Count;
#endif
            _connectorBlockers = new List<LaserInteractable>();
            for (var index = 0; index < allPaths.Count; index++)
            {
#if UNITY_EDITOR
                if (DebugCheckPathIteration(index))
                {
                    continue;
                }
#endif
                var path = allPaths[index];
                var reversePath = _allPathsReverse[index].ToList();
                reversePath.Reverse();

                LaserInteractable connectorBlocker = null;
                var isHitPhysicalBlocker = CheckPhysicalHitBlockerPath(path) || CheckPhysicalHitBlockerPath(reversePath);
                if (!isHitPhysicalBlocker)
                {
                    FindPathBlockers(path, _connectorBlockers, _conflictConnectors, out connectorBlocker);
                }
                
                GeneratePathLaser(generator.LaserColor, path, connectorBlocker, _conflictConnectors);
                GeneratePathLaser(conflictGenerator.LaserColor, reversePath, connectorBlocker, _conflictConnectors);
            }

            UpdateOutsidePathsConnections(allPaths);
            
            return isAllProceed && isAllConflictGeneratorProceed;
        }

        private bool IsAllGeneratorProceed(Generator generator, List<List<LaserInteractable>> allPaths)
        {
            var isAllProceed = true;
            
            var inputsToSkip = _alreadyCheckedGeneratorInputs[generator];
            foreach (var path in allPaths)
            {
                foreach (var conflictInput in generator.InputConnections)
                {
                    if (path.Contains(conflictInput) && !inputsToSkip.Contains(conflictInput))
                    {
                        inputsToSkip.Add(conflictInput);
                    }
                    else
                    {
                        isAllProceed = false;
                    }
                }
            }

            return isAllProceed;
        }

        private void UpdateOutsidePathsConnections(List<List<LaserInteractable>> allPaths)
        {
            var allBranches = _laserPathGenerator.FindOutsidePathBranches(allPaths);
            foreach (var (startLaserInteractable, branch) in allBranches)
            {
                if (!startLaserInteractable.CanShareLaser())
                {
                    continue;
                }
                
                var currentColor = startLaserInteractable.InputLaserColors[0];
                for (var index = 0; index < branch.Count; index++)
                {
                    if (index == branch.Count - 1)
                    {
                        break;
                    }

                    var current = branch[index];
                    var target = branch[index + 1];

                    if (_laserPathGenerator.IsLaserBlocked(current.LaserPoint, target.LaserPoint, out var hitPoint))
                    {
                        laserVFXController.DrawLaserEffectWithHit(currentColor, current.LaserPoint, hitPoint);
                        break;
                    }

                    target.AddInputColor(currentColor);

                    laserVFXController.DrawLaserEffect(currentColor, current.LaserPoint, target.LaserPoint);
                }
            }
        }

        private void FindPathBlockers(List<LaserInteractable> path, List<LaserInteractable> connectorBlockerBuffer, List<(LaserInteractable, LaserInteractable)> conflictConnectors, out LaserInteractable connectorBlocker)
        {
            connectorBlocker = null;
            var hasSameConflict = false;
            for (var index = 0; index < path.Count; index++)
            {
                if (index == path.Count - 1)
                {
                    continue;
                }
                
                var current = path[index];
                var target = path[index + 1];

                if (conflictConnectors.Contains((current, target)) || conflictConnectors.Contains((target, current)))
                {
                    hasSameConflict = true;
                }
            }
            
            
            var isConnectorBlockers = path.Count % 2 != 0;
            if (!isConnectorBlockers)
            {
                var firstIndex = path.Count / 2 - 1;
                var secondIndex = firstIndex + 1;

                if (!hasSameConflict)
                {
                    conflictConnectors.Add((path[secondIndex], path[firstIndex]));
                }
            }
            else
            {
                var index = path.Count / 2;
                connectorBlocker = path[index];
                
                connectorBlockerBuffer.Add(connectorBlocker);
            }
        }

        private void GenerateOpenLaserPath(Generator checkGenerator)
        {
            var paths = _laserPathGenerator.FindAllOpenPaths(checkGenerator);
            foreach (var path in paths)
            {
                var inputConnectionsToSkip = _alreadyCheckedGeneratorInputs[checkGenerator];
                var isAlreadyChecked = inputConnectionsToSkip.Contains(path[1]);
                if (isAlreadyChecked)
                {
                    continue;
                }
                        
                for (var index = 0; index < path.Count; index++)
                {
                    if (index == path.Count - 1)
                    {
                        continue;
                    }

                    var current = path[index];
                    var target = path[index + 1];

                    var isSucceed = LaserPathProcess(current, target, checkGenerator.LaserColor, null, null);
                    if (!isSucceed)
                    {
                        break;
                    }
                }
            }
        }

        private void GeneratePathLaser(ColorType color, List<LaserInteractable> pathInteractables, LaserInteractable connectorBlocker,
            List<(LaserInteractable, LaserInteractable)> lineConflictBlocker)
        {
            for (var index = 0; index < pathInteractables.Count; index++)
            {
                if (index == pathInteractables.Count - 1)
                {
                    break;
                }

                var current = pathInteractables[index];
                var target = pathInteractables[index + 1];
                var isSucceed = LaserPathProcess(current, target, color, connectorBlocker, lineConflictBlocker);
                if (!isSucceed)
                {
                    break;
                }
            }
        }

        private bool LaserPathProcess(LaserInteractable current, LaserInteractable target, 
            ColorType currentColor, LaserInteractable connectorBlocker, List<(LaserInteractable, LaserInteractable)> lineConflictBlocker)
        {
            if (_laserPathGenerator.IsLaserBlocked(current.LaserPoint, target.LaserPoint, out var hitPoint))
            {
                laserVFXController.DrawLaserEffectWithHit(currentColor, current.LaserPoint, hitPoint);
                return false;
            }

            var isCanConnectLaser = target.CanConnectColor(currentColor);
            var isBlocker = connectorBlocker != null && connectorBlocker == target || _connectorBlockers.Contains(target);
            var lineConflict = lineConflictBlocker != null && (lineConflictBlocker.Contains((current, target)) || lineConflictBlocker.Contains((target, current)));

            if (isBlocker)
            {
                laserVFXController.DrawLaserEffectWithHit(currentColor, current.LaserPoint, target.LaserPoint);
                target.AddInputColor(currentColor);
                    
                return false;
            }

            if (lineConflict)
            {
                laserVFXController.DrawConflictLaserEffect(currentColor, current, target);
                    
                return false;
            }

            target.AddInputColor(currentColor);
            laserVFXController.DrawLaserEffect(currentColor, current.LaserPoint, target.LaserPoint);


            return true;
        }

        private bool CheckPhysicalHitBlockerPath(List<LaserInteractable> pathInteractables)
        {
            var isHitPhysicalBlocker = false;
            for (var index = 0; index < pathInteractables.Count; index++)
            {
                if (index == pathInteractables.Count - 1)
                {
                    continue;
                }
                
                var pathInteractable = pathInteractables[index];
                var nextPathInteractable = pathInteractables[index + 1];

                if (_laserPathGenerator.IsLaserBlocked(pathInteractable.LaserPoint, nextPathInteractable.LaserPoint,
                        out _))
                {
                    isHitPhysicalBlocker = true;
                    
                    break;
                }
            }

            return isHitPhysicalBlocker;
        }

        private void ResetInteractables(IReadOnlyCollection<LaserInteractable> laserInteractables)
        {
            foreach (var laserInteractable in laserInteractables)
            {
                laserInteractable.Reset();
            }
        }

#if UNITY_EDITOR
        private bool DebugCheckPathIteration(int index)
        {
            if (onlyPathTargetDebug > 0 && index != onlyPathTargetDebug - 1)
            {
                return true;
            }

            if (maxPathCountDebug > 0 && index > maxPathCountDebug - 1)
            {
                return true;
            }

            return false;
        }
#endif
    }
}