using System.Collections.Generic;
using TalosTest.Tool;
using UnityEngine;
using UnityEngine.Pool;

namespace TalosTest.Visuals
{
    public class HitSparksEffectPool : MonoBehaviour
    {
        [SerializeField] private int defaultPoolSize = 5;
        [SerializeField] private int maxPoolSize = 10;
        [SerializeField] private GameObject hitMarkPrefab;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            InitializePool();
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<GameObject>(createFunc: CreateEffect, actionOnGet: OnGetElement, actionOnRelease: OnRelease, actionOnDestroy: OnDestroy, 
                collectionCheck: false, defaultCapacity: defaultPoolSize, maxSize: maxPoolSize);
        }

        public GameObject Get(Vector3 position)
        {
            var effect = _pool.Get();

            effect.transform.position = position;
            return effect;
        }

        private GameObject CreateEffect()
        {
            var effect = Instantiate(hitMarkPrefab, transform);
            effect.gameObject.SetActive(false);
            
            return effect;
        }

        public void Release(GameObject effect)
        {
            _pool.Release(effect);
        }

        private void OnGetElement(GameObject effect)
        {
            effect.gameObject.SetActive(true);
        }

        private void OnRelease(GameObject effect)
        {
            effect.gameObject.SetActive(false);
        }

        private void OnDestroy(GameObject effect)
        {
            Destroy(effect.gameObject);
        }
    }
}