using UnityEngine;

namespace TalosTest.Tool
{
    public struct BlockInfo
    {
        public Vector3 CollisionPoint;
        public readonly LaserSegment ConflictingSegment;
        public readonly LaserSegment OtherConflictingSegment;

        public BlockInfo(Vector3 point, LaserSegment conflictingSegment, LaserSegment otherConflictingSegment, bool isIntersection = false)
        {
            CollisionPoint = point;
            ConflictingSegment = conflictingSegment;
            OtherConflictingSegment = otherConflictingSegment;
        }
    }}