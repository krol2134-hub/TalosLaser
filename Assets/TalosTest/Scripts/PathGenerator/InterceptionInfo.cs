using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.PathGenerator
{
    public struct InterceptionInfo
    {
        public bool IsFound { get; }
        public LaserSegment Segment { get; }
        public Vector3 Point { get; }
        public float Distance { get; }

        public InterceptionInfo(bool isFound, LaserSegment segment, Vector3 point, float distance)
        {
            IsFound = isFound;
            Segment = segment;
            Point = point;
            Distance = distance;
        }
    }
}