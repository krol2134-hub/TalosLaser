using UnityEngine;

namespace TalosTest.PathGenerator
{
    public struct CollisionInfo
    {
        public Vector3 Point { get; }
        public float Distance { get; }

        public CollisionInfo(Vector3 point, float distance)
        {
            Point = point;
            Distance = distance;
        }
    }
}