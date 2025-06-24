using System.Collections.Generic;
using TalosTest.Tool;
using UnityEngine;
using UnityEngine.Pool;

namespace TalosTest.Visuals
{
    public class LaserEffectPool : MonoBehaviour
    {
        [SerializeField] private int defaultPoolSize = 15;
        [SerializeField] private int maxPoolSize = 50;
        [SerializeField] private List<LaserEffectByColorType> effectPrefabByColorTypes;

        private readonly Dictionary<ColorType, ObjectPool<LaserEffect>> _poolsByColor = new();

        private void Awake()
        {
            InitializePools();
        }

        private void OnDestroy()
        {
            foreach (var pool in _poolsByColor.Values)
            {
                pool?.Dispose();
            }
        }

        private void InitializePools()
        {
            foreach (var laserEffectByColorType in effectPrefabByColorTypes)
            {
                var pool = new ObjectPool<LaserEffect>(createFunc: CreateLaserEffect, actionOnGet: OnGetLineRenderer, actionOnRelease: OnReleaseLaserEffect, actionOnDestroy: OnDestroyLaserEffect, 
                    collectionCheck: false, defaultCapacity: defaultPoolSize, maxSize: maxPoolSize);
                
                _poolsByColor.Add(laserEffectByColorType.ColorType, pool);
                
                continue;

                LaserEffect CreateLaserEffect()
                {
                    var laserEffect = Instantiate(laserEffectByColorType.LaserEffectPrefab, transform);
                    laserEffect.gameObject.SetActive(false);
            
                    return laserEffect;
                }
            }
        }

        public LaserEffect Get(ColorType colorType, Vector3 currentPoint, Vector3 targetPoint)
        {
            var laserEffect = _poolsByColor[colorType].Get();

            laserEffect.transform.position = currentPoint;
            laserEffect.transform.LookAt(targetPoint);

            laserEffect.Setup(colorType);
            laserEffect.Clear();
            laserEffect.AddPoint(targetPoint);
            return laserEffect;
        }
        
        public void Release(LaserEffect laserEffect)
        {
            var colorType = laserEffect.ColorType;
            _poolsByColor[colorType].Release(laserEffect);
        }

        private void OnGetLineRenderer(LaserEffect laserEffect)
        {
            laserEffect.gameObject.SetActive(true);

            var lineRenderer = laserEffect.LineRenderer;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }

        private void OnReleaseLaserEffect(LaserEffect laserEffect)
        {
            laserEffect.gameObject.SetActive(false);
        }

        private void OnDestroyLaserEffect(LaserEffect laserEffect)
        {
            Destroy(laserEffect.gameObject);
        }
    }
}