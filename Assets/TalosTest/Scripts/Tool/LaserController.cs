using System.Collections.Generic;
using TalosTest.PathGenerator;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserController : MonoBehaviour
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
        private readonly HashSet<Vector3> _hitMarkPositions = new();

        private LaserPathGenerator _laserPathGenerator;
        
        private void Awake()
        {
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();

            _laserPathGenerator = new LaserPathGenerator(obstacleMask);
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
            DisplayLaserPaths();
        }

        private void ResetAll()
        {
            foreach (var laserInteractable in _previousFrameInteractables)
            {
                if (!_currentFrameInteractables.Contains(laserInteractable))
                {
                    laserInteractable.Reset();
                }
            }

            _previousFrameInteractables.Clear();
            foreach (var laser in _currentFrameInteractables)
            {
                _previousFrameInteractables.Add(laser);
            }
            
            laserVFXController.Clear();
            _currentFrameInteractables.Clear();
        }

        private void DisplayLaserPaths()
        {
            _hitMarkPositions.Clear();
            
            var paths = _laserPathGenerator.FindAllPaths(_generators, out var blockedInteractables, out var blockedSegments);

            foreach (var blocked in blockedInteractables)
            {
                _hitMarkPositions.Add(blocked.LaserPoint);
            }
            
            foreach (var (generator, pathList) in paths)
            {
                var currentColor = generator.Color;
                foreach (var laserPath in pathList)
                {
                    foreach (var segment in laserPath.Segments)
                    {
                        var startInteractable = segment.Start;
                        var endInteractable = segment.End;

                        if (segment.SegmentStatus is SegmentStatus.PhysicalBlocker or SegmentStatus.LineInception)
                        {
                            var collisionPoint = segment.CollisionInfo.Point;
                            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, collisionPoint);
                            _currentFrameInteractables.Add(segment.Start);
                            _hitMarkPositions.Add(collisionPoint);
                            break;
                        }
                        
                        var isLogicalConflictSegment = false;
                        foreach (var blockedSegment in blockedSegments)
                        {
                            if (blockedSegment.CheckMatchingBySides(segment))
                            {
                                isLogicalConflictSegment = true;
                                break;
                            }
                        }

                        if (isLogicalConflictSegment)
                        {
                            var targetPoint = CalculateMiddleConflictPosition(segment);
                            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, targetPoint);
                            _currentFrameInteractables.Add(segment.Start);
                            _hitMarkPositions.Add(targetPoint);
                            break;
                        }

                        if (blockedInteractables.Contains(endInteractable))
                        {
                            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, endInteractable.LaserPoint);
                            _currentFrameInteractables.Add(segment.Start);
                            break;
                        }
                        
                        endInteractable.AddInputColor(currentColor);
                        laserVFXController.DisplayLaserEffectConnection(generator.Color, segment.StartPoint, segment.EndPoint);

                        _currentFrameInteractables.Add(startInteractable);
                        _currentFrameInteractables.Add(endInteractable);
                    }
                }
            }
            
            foreach (var hitMarkPosition in _hitMarkPositions)
            {
                laserVFXController.DisplayHitMark(hitMarkPosition);
            }
        }

        private static Vector3 CalculateMiddleConflictPosition(LaserSegment segment)
        {
            var direction = segment.EndPoint - segment.StartPoint;
            var distance = direction.magnitude / 2;
            var targetPoint = segment.StartPoint + direction.normalized * distance;
            return targetPoint;
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
