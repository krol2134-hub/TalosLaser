using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.PathGenerator
{
    public struct CollisionInfo
    {
        public bool IsHit { get; }
        public LaserInteractable HitObject { get; }
        public Vector3 HitPoint { get; }
        public float Distance { get; }

        public CollisionInfo(bool isHit, LaserInteractable hitObject, Vector3 hitPoint, float distance)
        {
            IsHit = isHit;
            HitObject = hitObject;
            HitPoint = hitPoint;
            Distance = distance;
        }
    }
}