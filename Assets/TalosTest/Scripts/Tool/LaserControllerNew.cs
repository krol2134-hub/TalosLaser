using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserControllerNew : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private LaserVfxController laserVFXController;

#if UNITY_EDITOR
        [SerializeField] private int pathCountDebug;
        [SerializeField, Range(-1, 10)] private int maxPathCountDebug = -1;
        [SerializeField, Range(-1, 10)] private int onlyPathTargetDebug = -1;
#endif

        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private readonly HashSet<LaserInteractable> _previousFrameInteractables = new();
        private readonly HashSet<LaserInteractable> _currentFrameInteractables = new();

        private LaserPathGenerator _laserPathGenerator;
        private LaserInterceptionGenerator _laserInterceptionGenerator;

        private readonly Dictionary<Generator, List<List<LaserSegment>>> _segmentsByGenerator = new();

        private List<(Generator generator, LaserSegment segment)> _allSegments = new();

        private void Awake()
        {
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();

            foreach (var connector in _generators)
            {
                _segmentsByGenerator.Add(connector, new List<List<LaserSegment>>());
            }

            _laserPathGenerator = new LaserPathGenerator(obstacleMask);
            _laserInterceptionGenerator = new LaserInterceptionGenerator(obstacleMask);
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

        private void ResetAll()
        {
            foreach (var laser in _previousFrameInteractables)
            {
                if (!_currentFrameInteractables.Contains(laser))
                {
                    laser.Reset();
                }
            }

            _previousFrameInteractables.Clear();
            foreach (var laser in _currentFrameInteractables)
            {
                _previousFrameInteractables.Add(laser);
            }
            
            foreach (var kvp in _segmentsByGenerator)
            {
                foreach (var list in kvp.Value)
                {
                    list.Clear();
                }
                
                kvp.Value.Clear();
            }
            
            laserVFXController.Clear();
            _currentFrameInteractables.Clear();
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

            FindPathsByGenerators();
            SaveAllSegments();
            
            var blockPointInfosBySegment = _laserInterceptionGenerator.ProcessPathInterceptions(_allSegments);
            DisplayLaserPaths(new Dictionary<LaserSegment, InterceptionInfo>());
        }

        private void FindPathsByGenerators()
        {
            var pathsByGenerators = _laserPathGenerator.FindAllPaths(_generators);
            foreach (var (generator, segments) in pathsByGenerators)
            {
                _segmentsByGenerator[generator] = segments;
            }
        }

        private void SaveAllSegments()
        {
            _allSegments = new List<(Generator generator, LaserSegment segment)>();
            foreach (var kvp in _segmentsByGenerator)
            {
                foreach (var path in kvp.Value)
                {
                    foreach (var segment in path)
                    {
                        _allSegments.Add((kvp.Key, segment));
                    }
                }
            }
        }

        private void DisplayLaserPaths(Dictionary<LaserSegment, InterceptionInfo> blockPointInfosBySegment)
        {
            foreach (var (generator, pathList) in _segmentsByGenerator)
            {
                var currentColor = generator.LaserColor;
                foreach (var path in pathList)
                {
                    foreach (var segment in path)
                    {
                        var startInteractable = segment.Start;
                        var endInteractable = segment.End;
                        var laserStart = startInteractable.LaserPoint;
                        
                        Vector3 laserEnd;
                        if (blockPointInfosBySegment.TryGetValue(segment, out var interceptionInfo))
                        {
                            var dir = (endInteractable.LaserPoint - laserStart).normalized;
                            laserEnd = laserStart + dir * interceptionInfo.Distance;
                            laserVFXController.DisplayLaserEffect(generator.LaserColor, laserStart, laserEnd);
                            break;
                        }
                        
                        if (segment.ConnectionState == ConnectionState.PhysicalBlocker)
                        {
                            laserVFXController.DisplayLaserEffectWithHit(generator.LaserColor, segment.Start.LaserPoint, segment.BlockPoint);
                            _currentFrameInteractables.Add(segment.Start);
                            break;
                        }
                        
                        if (segment.ConnectionState == ConnectionState.Conflict)
                        {
                            laserVFXController.DisplayConflictLaserEffect(generator.LaserColor, segment.Start, segment.End);
                            _currentFrameInteractables.Add(segment.Start);
                            break;
                        }
                        
                        if (segment.ConnectionState == ConnectionState.Blocker)
                        {
                            laserVFXController.DisplayLaserEffectWithHit(generator.LaserColor, segment.Start.LaserPoint, segment.End.LaserPoint);
                            _currentFrameInteractables.Add(segment.Start);
                            break;
                        }
                        
                        laserEnd = segment.End.LaserPoint;
                        startInteractable.AddInputColor(currentColor);
                        laserVFXController.DisplayLaserEffectConnection(generator.LaserColor, laserStart, laserEnd);

                        _currentFrameInteractables.Add(startInteractable);
                    }
                }
            }

            foreach (var interceptionInfo in blockPointInfosBySegment.Values)
            {
                laserVFXController.DisplayHitMark(interceptionInfo.Point);
            }
        }

        private void CheckPhysicalBlockers(List<(Generator, LaserSegment)> allSegments, Dictionary<LaserSegment, BlockInfo> blockedSegmentInfos)
        {
            foreach (var (_, segment) in allSegments)
            {
                var from = segment.Start.LaserPoint;
                var to = segment.End.LaserPoint;
                var direction = (to - from).normalized;
                var distance = Vector3.Distance(from, to);

                if (!Physics.Raycast(from, direction, out var hit, distance, obstacleMask))
                {
                    continue;
                }
                
                if (blockedSegmentInfos.TryGetValue(segment, out var blockInfo))
                {
                    var distanceToIntersection = Vector3.Distance(from, blockInfo.CollisionPoint);
                    var distanceToHit = Vector3.Distance(from, hit.point);
                    
                    if (distanceToHit > distanceToIntersection + 0.01f)
                    {
                        continue;
                    }
                    else
                    {
                        blockedSegmentInfos.Remove(blockInfo.ConflictingSegment);
                        blockedSegmentInfos.Remove(blockInfo.OtherConflictingSegment);
                    }
                }

                //TODO Add other struct
                blockedSegmentInfos[segment] = new BlockInfo(hit.point, null, null);
            }
        }
        
#if UNITY_EDITOR
        private bool DebugCheckPathIteration(int index)
        {
            if (onlyPathTargetDebug > 0 && index != onlyPathTargetDebug - 1)
                return true;

            if (maxPathCountDebug > 0 && index > maxPathCountDebug - 1)
                return true;

            return false;
        }
#endif
    }
}
