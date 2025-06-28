using System.Collections.Generic;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserInterceptionGenerator
    {   
        private const int MaxIterations = 15;
        
        private readonly LayerMask _layerMaskObstacle;
        
        private readonly Dictionary<LaserSegment, List<LaserSegment>> _segmentsBlockedBy = new();
        private Dictionary<LaserSegment, float> _blockDistanceBySegment = new();
        private Dictionary<LaserSegment, InterceptionInfo> _blockPointInfosBySegment = new();
        
        public LaserInterceptionGenerator(LayerMask layerMaskObstacle)
        {
            _layerMaskObstacle = layerMaskObstacle;
        }
        
        public Dictionary<LaserSegment, InterceptionInfo> ProcessPathInterceptions(List<(Generator generator, LaserSegment segment)> allSegments)
        {
            bool changed;
            var iteration = 0;
            
            do
            {
                changed = false;
                _segmentsBlockedBy.Clear();

                var newSegmentBlockedDistances = new Dictionary<LaserSegment, float>();
                var newSegmentBlockPointInfos = new Dictionary<LaserSegment, InterceptionInfo>();

                for (var i = 0; i < allSegments.Count; i++)
                {
                    var (mainGenerator, mainSegment) = allSegments[i];
                    var mainColor = mainGenerator.LaserColor;

                    var start = mainSegment.Start.LaserPoint;
                    var end = mainSegment.End.LaserPoint;

                    var interceptionInfo = new InterceptionInfo(false, null, end, Vector3.Distance(start, end));

                    UpdateSegmentsIntersection(allSegments, i, mainColor, start, end, ref interceptionInfo);
                    CheckPhysicCollision(end, start, ref interceptionInfo);

                    if (interceptionInfo.IsFound)
                    {
                        var isChanged = !newSegmentBlockedDistances.TryGetValue(mainSegment, out var previousDistance) || 
                                        interceptionInfo.Distance < previousDistance - Mathf.Epsilon;

                        if (isChanged)
                        {
                            newSegmentBlockedDistances[mainSegment] = interceptionInfo.Distance;
                            newSegmentBlockPointInfos[mainSegment] = interceptionInfo;
                            changed = true;
                        }

                        var interceptionSegment = interceptionInfo.Segment;
                        if (interceptionSegment != null)
                        {
                            TryMakeInterception(mainSegment, interceptionInfo, newSegmentBlockedDistances);
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

        private void UpdateSegmentsIntersection(List<(Generator generator, LaserSegment segment)> allSegments, int i, ColorType mainColor, Vector3 start,
            Vector3 end, ref InterceptionInfo interceptionInfo)
        {
            for (var j = 0; j < allSegments.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }
                        
                var (otherGenerator, otherSegment) = allSegments[j];
                if (mainColor == otherGenerator.LaserColor)        
                {
                    continue;
                }

                var otherStart = otherSegment.Start.LaserPoint;
                var otherEnd = otherSegment.End.LaserPoint;
                CheckLaserIntersect(start, end, otherStart, otherEnd, otherSegment, ref interceptionInfo);
            }
        }

        private void CheckLaserIntersect(Vector3 start, Vector3 end, Vector3 otherStart, Vector3 otherEnd,
            LaserSegment otherSegment, ref InterceptionInfo interceptionInfo)
        {
            var checkIntersect = MathematicsHelper.CheckLasersIntersect(start, end, otherStart, otherEnd);
            if (!checkIntersect)
            {
                return;
            }
            
            var intersectionPoint = MathematicsHelper.GetLaserIntersectionPoint(start, end, otherStart, otherEnd);
            var distance = Vector3.Distance(start, intersectionPoint);
            var otherDistance = Vector3.Distance(otherStart, intersectionPoint);

            if (_blockDistanceBySegment.TryGetValue(otherSegment, out var previousBlockDistance))
            {
                if (previousBlockDistance < otherDistance - Mathf.Epsilon)
                {
                    return;
                }            
            }

            if (distance < interceptionInfo.Distance)
            {
                interceptionInfo = new InterceptionInfo(true, otherSegment, intersectionPoint, distance);
            }
        }

        private void CheckPhysicCollision(Vector3 end, Vector3 start, ref InterceptionInfo interceptionInfo)
        {
            var direction = (end - start).normalized;
            var maxDistance = Vector3.Distance(start, end);

            var isHit = Physics.Raycast(start, direction, out var hit, maxDistance, _layerMaskObstacle);
            if (!isHit)
            {
                return;
            }
            
            var distanceToHit = Vector3.Distance(start, hit.point);
            if (distanceToHit < interceptionInfo.Distance)
            {
                interceptionInfo = new InterceptionInfo(true, null, hit.point, distanceToHit);
            }
        }

        private void TryMakeInterception(LaserSegment segmentA, InterceptionInfo interceptionInfo, Dictionary<LaserSegment, float> newSegmentBlockedDistances)
        {
            var interceptionSegment = interceptionInfo.Segment;
            var interceptionDistance = interceptionInfo.Distance;
            
            _blockDistanceBySegment.TryGetValue(segmentA, out var previousBlockDistance);

            var isWasBlocked = _blockDistanceBySegment.ContainsKey(segmentA);
            var isPriorityNow = !isWasBlocked || interceptionDistance < previousBlockDistance - Mathf.Epsilon;
            
            var isOtherWasBlocked = _blockDistanceBySegment.ContainsKey(interceptionSegment);
            var isNewBlockerHasDistance = !newSegmentBlockedDistances.TryGetValue(interceptionSegment, out var newBlockerDistance);
            var isOtherPriorityNow = !isOtherWasBlocked || isNewBlockerHasDistance || interceptionDistance < newBlockerDistance - Mathf.Epsilon;

            if (isPriorityNow && isOtherPriorityNow)
            {
                AddInterception(segmentA, interceptionSegment);
            }
            else
            {
                RemoveInterception(segmentA, interceptionSegment);
            }
        }

        private void AddInterception(LaserSegment segment, LaserSegment interceptionSegment)
        {
            if (!_segmentsBlockedBy.ContainsKey(segment))
            {
                _segmentsBlockedBy[segment] = new List<LaserSegment>();
            }
            
            if (!_segmentsBlockedBy.ContainsKey(interceptionSegment))
            {
                _segmentsBlockedBy[interceptionSegment] = new List<LaserSegment>();
            }

            if (!_segmentsBlockedBy[segment].Contains(interceptionSegment))
            {
                _segmentsBlockedBy[segment].Add(interceptionSegment);
            }
                
            if (!_segmentsBlockedBy[interceptionSegment].Contains(segment))
            {
                _segmentsBlockedBy[interceptionSegment].Add(segment);
            }
        }

        private void RemoveInterception(LaserSegment segmentA, LaserSegment interceptionSegment)
        {
            if (_segmentsBlockedBy.TryGetValue(segmentA, out var segments))
            {
                segments.Remove(interceptionSegment);
            }

            if (_segmentsBlockedBy.TryGetValue(interceptionSegment, out var interceptionSegments))
            {
                interceptionSegments.Remove(segmentA);
            }
        }
    }
}