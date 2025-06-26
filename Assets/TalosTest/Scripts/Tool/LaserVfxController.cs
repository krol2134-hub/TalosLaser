using System.Collections.Generic;
using TalosTest.Pools;
using TalosTest.Visuals;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserVfxController : MonoBehaviour
    {
        [SerializeField] private LaserEffectPool laserEffectPool;
        [SerializeField] private GameObjectPool hitSparksEffectPool;
        [SerializeField] private float gizmosTime = 0f;

        private readonly Dictionary<(Vector3 start, Vector3 end, ColorType), LaserEffect> _activeLaserEffects = new();
        private readonly List<(Vector3 start, Vector3 end, ColorType)> _usedLasersBuffer = new();
        private readonly List<(Vector3 start, Vector3 end, ColorType)> _removeLasersBuffer = new();
        private readonly Dictionary<Vector3, GameObject> _activeHitEffects = new();
        private readonly HashSet<Vector3> _usedHitPoints = new();
        private readonly List<Vector3> _hitMarkPointsToRemove = new();
        
        public void Clear()
        {
            ClearEffects();
        }
        
        public void DrawConflictLaserEffect(ColorType currentColor, LaserInteractable current, LaserInteractable target)
        {
            var direction = target.LaserPoint - current.LaserPoint;
            var distance = direction.magnitude / 2;
            var targetPoint = current.LaserPoint + direction.normalized * distance;

            DrawLaserEffectWithHit(currentColor, current.LaserPoint, targetPoint);
        }

        public void DrawLaserEffectWithHit(ColorType currentColor, Vector3 startPoint, Vector3 targetPoint)
        {
            DisplayHitMark(targetPoint);

            DrawLaserEffect(currentColor, startPoint, targetPoint);
        }

        public void DrawLaserEffect(ColorType currentColor, Vector3 startPoint, Vector3 targetPoint)
        {
            Debug.DrawLine(startPoint, targetPoint, currentColor == ColorType.Blue ? Color.blue : Color.red, gizmosTime);
            
            var startTargetPoint = (startPoint, targetPoint, currentColor);

            if (_activeLaserEffects.TryGetValue(startTargetPoint, out var laserEffect))
            {
                UpdateLaserEffect(laserEffect, startPoint, targetPoint);
            }
            else
            {
                laserEffect = laserEffectPool.Get(currentColor, startPoint, targetPoint);
                UpdateLaserEffect(laserEffect, startPoint, targetPoint);
                _activeLaserEffects.Add((startPoint, targetPoint, currentColor), laserEffect);
            }

            _usedLasersBuffer.Add(startTargetPoint);
        }

        public void DisplayHitMark(Vector3 targetPoint)
        {
            _usedHitPoints.Add(targetPoint);

            if (_activeHitEffects.ContainsKey(targetPoint))
            {
                return;
            }
                
            var hitEffect = hitSparksEffectPool.Get(targetPoint);
            _activeHitEffects[targetPoint] = hitEffect;
        }

        private void UpdateLaserEffect(LaserEffect laserEffect, Vector3 startPoint, Vector3 targetPoint)
        {
            laserEffect.transform.position = startPoint;
            laserEffect.transform.LookAt(targetPoint);
        }
        
        
        private void ClearEffects()
        {
            _removeLasersBuffer.Clear();
            _usedLasersBuffer.Clear();

            foreach (var activeLaserEffect in _activeLaserEffects)
            {
                if (_usedLasersBuffer.Contains(activeLaserEffect.Key))
                {
                    continue;
                }
                
                laserEffectPool.Release(activeLaserEffect.Value);
                _removeLasersBuffer.Add(activeLaserEffect.Key);
            }

            foreach (var key in _removeLasersBuffer)
            {
                _activeLaserEffects.Remove(key);
            }

            _hitMarkPointsToRemove.Clear();
            foreach (var kvp in _activeHitEffects)
            {
                if (!_usedHitPoints.Contains(kvp.Key))
                {
                    kvp.Value.SetActive(false);
                    hitSparksEffectPool.Release(kvp.Value);
                    _hitMarkPointsToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in _hitMarkPointsToRemove)
            {
                _activeHitEffects.Remove(key);
            }

            _usedHitPoints.Clear();
        }
    }
}