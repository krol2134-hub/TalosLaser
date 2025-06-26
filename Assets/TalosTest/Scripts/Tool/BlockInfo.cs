using UnityEngine;

namespace TalosTest.Tool
{
    public struct BlockInfo
    {
        public Vector3 CollisionPoint;
        public LaserSegment ConflictingSegment;

        public BlockInfo(Vector3 point, LaserSegment conflict)
        {
            CollisionPoint = point;
            ConflictingSegment = conflict;
        }
    }
}