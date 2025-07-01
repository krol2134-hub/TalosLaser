using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.PathGenerator
{
    public class LaserSegment
    {
        public LaserInteractable Start { get; }
        public LaserInteractable End { get; }
        public Vector3 StartPoint => Start.LaserPoint;
        public Vector3 EndPoint => End.LaserPoint;
        
        public ColorType ColorType { get; }
        public SegmentStatus SegmentStatus { get; private set; }
        
        public CollisionInfo CollisionInfo { get; private set;}

        public LaserSegment(LaserInteractable start, LaserInteractable end, SegmentStatus segmentStatus, ColorType colorType,
            CollisionInfo collisionInfo = default)
        {
            Start = start;
            End = end;
            ColorType = colorType;
            SegmentStatus = segmentStatus;
            CollisionInfo = collisionInfo;
        }

        public void UpdateStatusState(SegmentStatus newState, CollisionInfo collisionInfo)
        {
            SegmentStatus = newState;
            CollisionInfo = collisionInfo;
        }

        public bool CheckMatchingBySides(LaserSegment otherSegment)
        { 
            var otherStart = otherSegment.Start;
            var otherEnd = otherSegment.End;
            
            return CheckMatchingBySides(otherStart, otherEnd);
        }
        
        public bool CheckMatchingBySides(LaserInteractable otherStart, LaserInteractable otherEnd)
        {
            var isOneSideMatch = Start == otherStart && End == otherEnd;
            var isOtherSideMatch = Start == otherEnd && End == otherStart;
            
            return isOneSideMatch || isOtherSideMatch;
        }

        public override string ToString() => $"{ColorType} => {Start} → {End}";
    }
}