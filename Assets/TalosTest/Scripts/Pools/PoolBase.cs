using UnityEngine;
using UnityEngine.Pool;

namespace TalosTest.Pools
{
    public abstract class PoolBase<T> : MonoBehaviour 
        where T : Object
    {
        [SerializeField] private int defaultPoolSize = 5;
        [SerializeField] private int maxPoolSize = 10;
        [SerializeField] private T prefab;

        private ObjectPool<T> _pool;

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
            _pool = new ObjectPool<T>(createFunc: CreateEffect, actionOnGet: OnGetElement,
                actionOnRelease: OnRelease, actionOnDestroy: OnDestroyFromPool,
                collectionCheck: false, defaultCapacity: defaultPoolSize, maxSize: maxPoolSize);
        }

        public virtual T Get(Vector3 position)
        {
            var effect = _pool.Get();
            return effect;
        }

        protected virtual T CreateEffect()
        {
            var effect = Instantiate(prefab, transform);
            return effect;
        }

        public virtual void Release(T effect)
        {
            _pool.Release(effect);
        }

        protected abstract void OnGetElement(T effect);

        protected abstract void OnRelease(T effect);

        protected abstract void OnDestroyFromPool(T effect);
    }
}