using System.Collections.Generic;
using System.Linq;
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
        
        public Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo> ProcessPathInterceptions(Dictionary<Generator, List<LaserPath>> allPaths)
        {
            _allPaths = allPaths;
            
            bool changed;
            var iteration = 0;
            
            do
            {
                _segmentsBlockedBy.Clear();
                
                changed = false;

                var newSegmentBlockedDistances = new Dictionary<(LaserInteractable, LaserInteractable), float>();
                var newSegmentBlockPointInfos = new Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo>();
                
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
                                
                                MakeInterception(currentSegment, interceptionInfo, newSegmentBlockedDistances, newSegmentBlockPointInfos);
                                changed = true;
                            }
                        }
                    }
                }
                
                _blockDistanceBySegment = newSegmentBlockedDistances;
                _blockPointInfosBySegment = newSegmentBlockPointInfos;

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
            
            var checkIntersect = MathematicsUtil.CheckLasersIntersect(currentStartPoint, currentEndPoint, conflictStartPoint, conflictEndPoint);
            if (!checkIntersect)
            {
                return;
            }
            
            var intersectionPoint = MathematicsUtil.GetLaserIntersectionPoint(currentStartPoint, currentEndPoint, conflictStartPoint, conflictEndPoint);
            var distanceFromCurrent = Vector3.Distance(currentStartPoint, intersectionPoint);
            var distanceFromConflict = Vector3.Distance(conflictStartPoint, intersectionPoint);
            
            var currentKey = (currentSegment.Start, currentSegment.End);
            var reverseKey = (currentSegment.End, currentSegment.Start);
            var conflictKey = (conflictSegment.Start, conflictSegment.End);
            var conflictReverseKey = (conflictSegment.End, conflictSegment.Start);

            if (_blockDistanceBySegment.ContainsKey(reverseKey) || 
                _blockDistanceBySegment.ContainsKey(conflictReverseKey))
            {
                return;
            }
            
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

        private void MakeInterception(LaserSegment currentSegment, InterceptionInfo interceptionInfo,
            Dictionary<(LaserInteractable, LaserInteractable), float> newSegmentBlockedDistances, 
            Dictionary<(LaserInteractable, LaserInteractable), InterceptionInfo> newSegmentBlockPointInfos)
        {
            var conflictSegment = interceptionInfo.Segment;
            var intersectionPoint = interceptionInfo.Point;
            var interceptionDistance = interceptionInfo.Distance;
            var distanceToIntersection = Vector3.Distance(conflictSegment.StartPoint, intersectionPoint);

            var currentKey = (currentSegment.Start, currentSegment.End);
            var reverseKey = (currentSegment.End, currentSegment.Start);
            var conflictKey = (conflictSegment.Start, conflictSegment.End);
            var conflictReverseKey = (conflictSegment.End, conflictSegment.Start);

            if (newSegmentBlockedDistances.ContainsKey(reverseKey) || newSegmentBlockedDistances.ContainsKey(conflictReverseKey))
            {
                return;
            }            
            
            var isCurrentBlocked = currentSegment.SegmentStatus == SegmentStatus.PhysicalBlocker;
            var isConflictBlocked = conflictSegment.SegmentStatus == SegmentStatus.PhysicalBlocker;

            var hasCurrentPriority = !isCurrentBlocked || interceptionDistance < currentSegment.CollisionInfo.Distance - Mathf.Epsilon;
            var hasConflictPriority = !isConflictBlocked || distanceToIntersection < conflictSegment.CollisionInfo.Distance - Mathf.Epsilon;
            
            if (hasCurrentPriority && hasConflictPriority)
            {
                var isPriorityNow = !newSegmentBlockedDistances.TryGetValue(currentKey, out var existingDistance) || 
                                     interceptionDistance < existingDistance - Mathf.Epsilon;

                var isConflictPriorityNow = !newSegmentBlockedDistances.TryGetValue(conflictKey, out var existingConflictDistance) || 
                                             distanceToIntersection < existingConflictDistance - Mathf.Epsilon;

                if (isPriorityNow && isConflictPriorityNow)
                {
                    RemoveInterception(currentSegment, conflictSegment);
                    AddInterception(currentSegment, conflictSegment);

                    newSegmentBlockedDistances[currentKey] = interceptionDistance;
                    newSegmentBlockPointInfos[currentKey] = interceptionInfo;

                    newSegmentBlockedDistances[conflictKey] = distanceToIntersection;
                    newSegmentBlockPointInfos[conflictKey] = new InterceptionInfo(true, currentSegment, intersectionPoint, distanceToIntersection);
                }
            }
            else
            {
                RemoveInterception(currentSegment, conflictSegment);
                
                newSegmentBlockedDistances.Remove(currentKey);
                newSegmentBlockedDistances.Remove(conflictKey);

                newSegmentBlockPointInfos.Remove(currentKey);
                newSegmentBlockPointInfos.Remove(conflictKey);
            }
        }

        private void AddInterception(LaserSegment currentSegment, LaserSegment conflictSegment)
        {
            AddSegmentChain(currentSegment);
            AddSegmentChain(conflictSegment);
        }

        private void AddSegmentChain(LaserSegment sourceSegment)
        {
            var pathsWithIndex = FindPathContainingSegment(sourceSegment);
            if (pathsWithIndex is not { Count: > 0 }) 
                return;

            foreach (var (startIndex, path) in pathsWithIndex)
            {
                if (startIndex == -1)
                {
                    return;
                }
            
                for (var i = startIndex; i < path.Segments.Count; i++)
                {
                    var segment = path.Segments[i];
                    var key = (segment.Start, segment.End);
                
                    if (!_segmentsBlockedBy.Contains(key))
                    {
                        _segmentsBlockedBy.Add(key);
                    }
                }   
            }
        }

        private void RemoveInterception(LaserSegment currentSegment, LaserSegment conflictSegment)
        {
            RemoveSegmentChain(currentSegment, conflictSegment);
            RemoveSegmentChain(conflictSegment, currentSegment);
        }

        private void RemoveSegmentChain(LaserSegment sourceSegment, LaserSegment targetSegment)
        {
            var pathsWithIndex = FindPathContainingSegment(sourceSegment);
            if (pathsWithIndex is not { Count: > 0 })
            {
                return;
            }
            
            foreach (var (startIndex, path) in pathsWithIndex)
            {
                if (startIndex == -1)
                {
                    return;
                }

                for (var i = startIndex; i < path.Segments.Count; i++)
                {
                    var segment = path.Segments[i];
                    var key = (segment.Start, segment.End);

                    if (!_segmentsBlockedBy.Contains(key))
                    {
                        _segmentsBlockedBy.Remove(key);
                    }
                }
            }
        }
        
        private List<(int, LaserPath)> FindPathContainingSegment(LaserSegment segment)
        {
            var pathsWithIndex = new List<(int, LaserPath)>();
            foreach (var laserPaths in _allPaths.Values)
            {
                for (var index = 0; index < laserPaths.Count; index++)
                {
                    var path = laserPaths[index];
                    if (path.Segments.Any(s => s.Start == segment.Start && s.End == segment.End))
                    {
                        pathsWithIndex.Add((index, path));
                    }
                }
            }
            
            return pathsWithIndex;
        }
    }
}