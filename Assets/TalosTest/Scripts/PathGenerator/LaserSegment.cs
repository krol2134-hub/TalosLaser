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
        public ConnectionState ConnectionState { get; private set; }
        public Vector3 BlockPoint { get; private set;}
        public float BlockDistance { get; private set;}

        public LaserSegment(LaserInteractable start, LaserInteractable end, ConnectionState connectionState, ColorType colorType = default,
            Vector3 blockPoint = default, float blockDistance = default)
        {
            Start = start;
            End = end;
            ColorType = colorType;
            ConnectionState = connectionState;
            BlockDistance = blockDistance;
            BlockPoint = blockPoint;
        }

        public void UpdateBlockState(ConnectionState newState, Vector3 blockPoint, float blockDistance)
        {
            ConnectionState = newState;
            BlockPoint = blockPoint;
            BlockDistance = blockDistance;
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