using System.Collections.Generic;
using TalosTest.PathGenerator;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserController : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private LaserVfxController laserVFXController;
        [SerializeField] private int updateFrameRate = 10;

#if UNITY_EDITOR
        [SerializeField] private int pathCountDebug;
        [SerializeField, Range(-1, 10)] private int maxPathCountDebug = -1;
        [SerializeField, Range(-1, 10)] private int onlyPathTargetDebug = -1;
#endif
        private readonly HashSet<LaserInteractable> _previousFrameInteractables = new();
        private readonly HashSet<LaserInteractable> _currentFrameInteractables = new();
        private readonly HashSet<Vector3> _hitMarkPositions = new();

        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private LaserPathGenerator _laserPathGenerator;

        private int _frameCount;

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
            _frameCount++;

            if (_frameCount % 10 != 0)
            {
                return;
            }
            
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

                        _currentFrameInteractables.Add(startInteractable);

                        if (TryApplyPhysicalBlocks(segment, generator))
                        {
                            break;
                        }
                        
                        if (TryApplySegmentLogicalConflict(blockedSegments, segment, generator))
                        {
                            break;
                        }

                        if (TryApplyBlockLogicalConflict(blockedInteractables, endInteractable, generator, segment))
                        {
                            break;
                        }
                        
                        if (TryApplyLaserToReceiver(endInteractable, currentColor, generator, segment))
                        {
                            break;
                        }
                        
                        laserVFXController.DisplayLaserEffectConnection(generator.Color, segment.StartPoint, segment.EndPoint);
                    }
                }
            }
            
            DisplayHitMarks();
        }
        
        private bool TryApplyPhysicalBlocks(LaserSegment segment, Generator generator)
        {
            if (segment.SegmentStatus is not (SegmentStatus.PhysicalBlocker or SegmentStatus.LineInception))
            {
                return false;
            }       
            
            var collisionPoint = segment.CollisionInfo.Point;
            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, collisionPoint);
            _hitMarkPositions.Add(collisionPoint);
            
            return true;
        }

        private bool TryApplySegmentLogicalConflict(HashSet<LaserSegment> blockedSegments, LaserSegment segment, Generator generator)
        {
            var isLogicalConflictSegment = false;
            foreach (var blockedSegment in blockedSegments)
            {
                if (blockedSegment.CheckMatchingTwoSide(segment))
                {
                    isLogicalConflictSegment = true;
                    break;
                }
            }

            if (!isLogicalConflictSegment)
            {
                return false;
            }
            
            var targetPoint = CalculateMiddleConflictPosition(segment);
            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, targetPoint);
            _hitMarkPositions.Add(targetPoint);
            
            return true;
        }

        private static Vector3 CalculateMiddleConflictPosition(LaserSegment segment)
        {
            var direction = segment.EndPoint - segment.StartPoint;
            var distance = direction.magnitude / 2;
            var targetPoint = segment.StartPoint + direction.normalized * distance;
            return targetPoint;
        }

        private bool TryApplyBlockLogicalConflict(HashSet<LaserInteractable> blockedInteractables, LaserInteractable endInteractable,
            Generator generator, LaserSegment segment)
        {
            if (!blockedInteractables.Contains(endInteractable))
            {
                return false;
            }
            
            laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, endInteractable.LaserPoint);
            
            return true;
        }

        private bool TryApplyLaserToReceiver(LaserInteractable endInteractable, ColorType currentColor, Generator generator,
            LaserSegment segment)
        {
            _currentFrameInteractables.Add(endInteractable);
            if (endInteractable.Type == InteractableType.Receiver)
            {
                if (endInteractable.CanConnectColor(currentColor))
                {
                    endInteractable.AddInputColor(currentColor);
                    laserVFXController.DisplayLaserEffectConnection(generator.Color, segment.StartPoint, segment.EndPoint);
                }
                else
                {
                    laserVFXController.DisplayLaserEffect(generator.Color, segment.StartPoint, segment.EndPoint);
                    _hitMarkPositions.Add(endInteractable.LaserPoint);
                }
                
                return true;
            }
            
            return false;
        }

        private void DisplayHitMarks()
        {
            foreach (var hitMarkPosition in _hitMarkPositions)
            {
                laserVFXController.DisplayHitMark(hitMarkPosition);
            }
        }
        
#if UNIY_EDITOR
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
