using System.Collections.Generic;
using System.Linq;
using TalosTest.Interactables;
using TalosTest.Tool;
using TalosTest.Utils;
using UnityEngine;

namespace TalosTest.PathGenerator
{
    public class PathInterceptionGenerator
    {   
        private const int MaxIterations = 15;
        
        private readonly List<(LaserInteractable, LaserInteractable)> _segmentsBlockedBy = new();
        
        private Dictionary<(LaserInteractable, LaserInteractable), float> _blockDistanceBySegment = new();
        private Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo> _blockPointInfosBySegment = new();
        
        private Dictionary<Generator, List<LaserPath>> _allPaths;
        private Dictionary<(LaserInteractable, LaserInteractable), float> _newSegmentBlockedDistances;
        private Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo> _newSegmentBlockPointInfos;
        
        public Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo> ProcessPathInterceptions(Dictionary<Generator, List<LaserPath>> allPaths)
        {
            _allPaths = allPaths;
            
            bool changed;
            var iteration = 0;
            
            do
            {
                _segmentsBlockedBy.Clear();
                
                changed = false;

                _newSegmentBlockedDistances = new Dictionary<(LaserInteractable, LaserInteractable), float>();
                _newSegmentBlockPointInfos = new Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo>();
                
                foreach (var (generator, laserPaths) in _allPaths)
                {
                    foreach (var laserPath in laserPaths)
                    {
                        var allSegments = laserPath.Segments;
                        foreach (var currentSegment in allSegments)
                        {
                            if (CheckBlocking(currentSegment))
                            {
                                continue;
                            }
                            
                            var startDistance = Vector3.Distance(currentSegment.StartPoint, currentSegment.EndPoint);
                            var interceptionInfo = new InterceptionInfo(false, null, currentSegment.StartPoint, startDistance);
                            UpdateSegmentsIntersection(generator, currentSegment, ref interceptionInfo);
                            
                            if (interceptionInfo.IsFound)
                            {
                                var interceptionSegment = interceptionInfo.Segment;
                                if (CheckBlocking(interceptionSegment))
                                {
                                    continue;
                                }
                                
                                MakeInterception(currentSegment, interceptionInfo);
                                changed = true;
                            }
                        }
                    }
                }
                
                _blockDistanceBySegment = _newSegmentBlockedDistances;
                _blockPointInfosBySegment = _newSegmentBlockPointInfos;
                
                iteration++;
            } 
            while (changed && iteration < MaxIterations);
            
            return _blockPointInfosBySegment;
        }

        private bool CheckBlocking(LaserSegment interceptionSegment)
        {
            return _segmentsBlockedBy.Contains((interceptionSegment.Start, interceptionSegment.End));
        }

        private void UpdateSegmentsIntersection(Generator currentGenerator, LaserSegment currentSegment, ref InterceptionInfo interceptionInfo)
        {
            foreach (var (otherGenerator, laserPaths) in _allPaths)
            {
                foreach (var laserPath in laserPaths)
                {
                    var allSegments = laserPath.Segments;
                    foreach (var otherSegment in allSegments)
                    {
                        if (currentGenerator.Color == otherGenerator.Color)
                        {
                            continue;
                        }

                        if (CanSkipConnectedSegment(currentSegment, otherSegment))
                        {
                            continue;
                        }

                        CheckLaserIntersect(currentSegment, otherSegment, ref interceptionInfo);
                    }
                }
            }
        }

        private bool CanSkipConnectedSegment(LaserSegment currentSegment, LaserSegment conflictSegment)
        {
            return currentSegment.Start == conflictSegment.Start ||
                   currentSegment.Start == conflictSegment.End ||
                   currentSegment.End == conflictSegment.Start ||
                   currentSegment.End == conflictSegment.End;
        }

        private void CheckLaserIntersect(LaserSegment currentSegment, LaserSegment conflictSegment, ref InterceptionInfo interceptionInfo)
        {
            var currentStartPoint = currentSegment.StartPoint;
            var currentEndPoint = currentSegment.EndPoint;
            
            var conflictStartPoint = conflictSegment.StartPoint;
            var conflictEndPoint = conflictSegment.EndPoint;
            
            var checkIntersect = MathematicsUtils.CheckLasersIntersect(currentStartPoint, currentEndPoint, conflictStartPoint, conflictEndPoint);
            if (!checkIntersect)
            {
                return;
            }
            
            var intersectionPoint = MathematicsUtils.GetLaserIntersectionPoint(currentStartPoint, currentEndPoint, conflictStartPoint, conflictEndPoint);
            var distanceFromCurrent = Vector3.Distance(currentStartPoint, intersectionPoint);
            var distanceFromConflict = Vector3.Distance(conflictStartPoint, intersectionPoint);
            
            var conflictKey = (conflictSegment.Start, conflictSegment.End);
            var conflictReverseKey = (conflictSegment.End, conflictSegment.Start);
            
            var isHasPriorityDistance = _blockDistanceBySegment.TryGetValue(conflictKey, out var previousDist) || _blockDistanceBySegment.TryGetValue(conflictReverseKey, out previousDist);
            if (isHasPriorityDistance && previousDist < distanceFromConflict - Mathf.Epsilon)
            {
                return;
            }

            if (distanceFromCurrent < interceptionInfo.Distance)
            {
                interceptionInfo = new InterceptionInfo(true, conflictSegment, intersectionPoint, distanceFromCurrent);
            }
        }

        private void MakeInterception(LaserSegment currentSegment, InterceptionInfo interceptionInfo)
        {
            var conflictSegment = interceptionInfo.Segment;
            var intersectionPoint = interceptionInfo.Point;
            var interceptionDistance = interceptionInfo.Distance;
            var distanceToIntersection = Vector3.Distance(conflictSegment.StartPoint, intersectionPoint);

            var currentKey = (currentSegment.Start, currentSegment.End);
            var conflictKey = (conflictSegment.Start, conflictSegment.End);
            
            var isCurrentBlocked = currentSegment.SegmentStatus == SegmentStatus.PhysicalBlocker;
            var isConflictBlocked = conflictSegment.SegmentStatus == SegmentStatus.PhysicalBlocker;

            var hasCurrentPriority = !isCurrentBlocked || interceptionDistance < currentSegment.CollisionInfo.Distance - Mathf.Epsilon;
            var hasConflictPriority = !isConflictBlocked || distanceToIntersection < conflictSegment.CollisionInfo.Distance - Mathf.Epsilon;
            
            if (hasCurrentPriority && hasConflictPriority)
            {
                var isPriorityNow = !_newSegmentBlockedDistances.TryGetValue(currentKey, out var existingDistance) || 
                                     interceptionDistance < existingDistance - Mathf.Epsilon;

                var isConflictPriorityNow = !_newSegmentBlockedDistances.TryGetValue(conflictKey, out var existingConflictDistance) || 
                                             distanceToIntersection < existingConflictDistance - Mathf.Epsilon;
                
                if (isPriorityNow && isConflictPriorityNow)
                {
                    var conflictInterception = new InterceptionInfo(true, currentSegment, intersectionPoint, distanceToIntersection);
                    AddInterception(currentSegment, interceptionInfo);
                    AddInterception(conflictSegment, conflictInterception);
                }
            }
            else
            {
                RemoveInterception(currentSegment);
                RemoveInterception(conflictSegment);
            }
        }
        
        private void TryApplyOneWayInterception(LaserSegment segment, InterceptionInfo interceptionInfo)
        {
            var interceptionDistance = interceptionInfo.Distance;

            var isCurrentBlocked = segment.SegmentStatus == SegmentStatus.PhysicalBlocker;

            var hasCurrentPriority = !isCurrentBlocked || interceptionDistance < segment.CollisionInfo.Distance - Mathf.Epsilon;

            if (hasCurrentPriority)
            {
                var currentPreset = (segment.Start, segment.End);
                _newSegmentBlockedDistances[currentPreset] = interceptionDistance;
                _newSegmentBlockPointInfos[currentPreset] = interceptionInfo;

                if (!_segmentsBlockedBy.Contains(currentPreset))
                {
                    _segmentsBlockedBy.Add(currentPreset);
                }            }
        }

        private void AddInterception(LaserSegment sourceSegment, InterceptionInfo interceptionInfo)
        {
            var segmentPreset = (sourceSegment.Start, sourceSegment.End);
            
            _newSegmentBlockedDistances[segmentPreset] = interceptionInfo.Distance;
            _newSegmentBlockPointInfos[segmentPreset] = interceptionInfo;

            if (!_segmentsBlockedBy.Contains(segmentPreset))
            {
                _segmentsBlockedBy.Add(segmentPreset);
            }

            var reverseSegment = GetReversedSegment(sourceSegment);
            if (reverseSegment == null)
            {
                return;
            }
            
            var reverseKey = (reverseSegment.Start, reverseSegment.End);
            if (_segmentsBlockedBy.Contains(reverseKey))
            {
                return;
            }
            
            var reverseDistance = Vector3.Distance(reverseSegment.StartPoint, interceptionInfo.Point);
            var reverseInfo = new InterceptionInfo(true, interceptionInfo.Segment, interceptionInfo.Point, reverseDistance);

            TryApplyOneWayInterception(reverseSegment, reverseInfo);
        }

        private void RemoveInterception(LaserSegment sourceSegment)
        {
            var segmentPreset = (sourceSegment.Start, sourceSegment.End);
            
            _newSegmentBlockedDistances.Remove(segmentPreset);
            _newSegmentBlockPointInfos.Remove(segmentPreset);
            
            if (_segmentsBlockedBy.Contains(segmentPreset))
            {
                _segmentsBlockedBy.Remove(segmentPreset);
            }
            
            var reverseSegment = GetReversedSegment(sourceSegment);
            if (reverseSegment == null)
            {
                return;
            }
            
            var reversePreset = (reverseSegment.Start, reverseSegment.End);
            if (_segmentsBlockedBy.Contains(reversePreset))
            {
                _segmentsBlockedBy.Remove(reversePreset);
            }
        }

        private LaserSegment GetReversedSegment(LaserSegment sourceSegment)
        {
            foreach (var laserPaths in _allPaths.Values)
            {
                foreach (var path in laserPaths)
                {
                    var reverseSegment = path.Segments.FirstOrDefault(s =>
                        s.Start == sourceSegment.End && s.End == sourceSegment.Start);

                    if (reverseSegment != null)
                    {
                        return reverseSegment;
                    }
                }
            }

            return null;
        }
    }
}