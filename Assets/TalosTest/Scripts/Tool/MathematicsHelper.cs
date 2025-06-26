using System.Collections.Generic;
using UnityEngine;

namespace TalosTest.Tool
{
    public static class MathematicsHelper
    {
        public static Dictionary<LaserSegment, BlockInfo> GetLaserSegmentBlockers(List<(Generator, LaserSegment)> allSegments)
        {
            Dictionary<LaserSegment, BlockInfo> blockedSegmentInfos = new();
            for (var i = 0; i < allSegments.Count; i++)
            {
                for (var j = i + 1; j < allSegments.Count; j++)
                {
                    var (genA, segA) = allSegments[i];
                    var (genB, segB) = allSegments[j];

                    if (genA == genB)
                    {
                        continue;
                    }

                    if (CheckLasersIntersect(segA, segB))
                    {
                        var intersection = GetLaserIntersectionPoint(segA, segB);

                        blockedSegmentInfos[segA] = new BlockInfo(intersection, segB, segA);
                        blockedSegmentInfos[segB] = new BlockInfo(intersection, segA, segB);
                    }
                }
            }

            return blockedSegmentInfos;
        }
        
        public static bool CheckLasersIntersect(LaserSegment a, LaserSegment b)
        {
            var a1 = a.From.LaserPoint;
            var a2 = a.To.LaserPoint;
            var b1 = b.From.LaserPoint;
            var b2 = b.To.LaserPoint;

            return LinesIntersect(a1, a2, b1, b2);
        }

        public static Vector3 GetLaserIntersectionPoint(LaserSegment firstSegment, LaserSegment secondSegment)
        {
            var p1 = firstSegment.From.LaserPoint;
            var p2 = firstSegment.To.LaserPoint;
            var q1 = secondSegment.From.LaserPoint;
            var q2 = secondSegment.To.LaserPoint;

            ClosestPointsOnTwoLines(out var c1, out var c2, p1, (p2 - p1).normalized, q1, (q2 - q1).normalized);
            return (c1 + c2) * 0.5f;
        }

        public static bool ClosestPointsOnTwoLines(out Vector3 pointLine1, out Vector3 pointLine2,
            Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            pointLine1 = Vector3.zero;
            pointLine2 = Vector3.zero;

            var a = Vector3.Dot(lineVec1, lineVec1);
            var b = Vector3.Dot(lineVec1, lineVec2);
            var e = Vector3.Dot(lineVec2, lineVec2);

            var d = a * e - b * b;
            if (Mathf.Abs(d) < 0.0001f)
            {
                return false;
            }

            var r = linePoint1 - linePoint2;
            var c = Vector3.Dot(lineVec1, r);
            var f = Vector3.Dot(lineVec2, r);

            var s = (b * f - c * e) / d;
            var t = (a * f - c * b) / d;

            pointLine1 = linePoint1 + lineVec1 * s;
            pointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        public static bool LinesIntersect(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            var a = new Vector2(p1.x, p1.z);
            var b = new Vector2(p2.x, p2.z);
            var c = new Vector2(q1.x, q1.z);
            var d = new Vector2(q2.x, q2.z);

            return CheckLineSegmentsIntersect(a, b, c, d);
        }

        public static bool CheckLineSegmentsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2)
        {
            float o1 = Orientation(p, p2, q);
            float o2 = Orientation(p, p2, q2);
            float o3 = Orientation(q, q2, p);
            float o4 = Orientation(q, q2, p2);

            return !Mathf.Approximately(o1, o2) && !Mathf.Approximately(o3, o4);
        }

        public static int Orientation(Vector2 a, Vector2 b, Vector2 c)
        {
            var val = (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);
            if (Mathf.Approximately(val, 0))
            {
                return 0;
            }
            
            return val > 0 ? 1 : 2;
        }
    }
}