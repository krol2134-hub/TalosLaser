using UnityEngine;

namespace TalosTest.Tool
{
    public static class MathematicsHelper
    {
        public static bool CheckLasersIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            var a = new Vector2(a1.x, a1.z);
            var b = new Vector2(a2.x, a2.z);
            var c = new Vector2(b1.x, b1.z);
            var d = new Vector2(b2.x, b2.z);
            return CheckLineSegmentsIntersect(a, b, c, d);
        }

        public static Vector3 GetLaserIntersectionPoint(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            ClosestPointsOnTwoLines(out var c1, out var c2, a1, (a2 - a1).normalized, b1, (b2 - b1).normalized);
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