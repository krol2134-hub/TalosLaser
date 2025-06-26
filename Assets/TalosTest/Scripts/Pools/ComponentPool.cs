using UnityEngine;

namespace TalosTest.Pools
{
    public class ComponentPool<T> : PoolBase<T> where T : Component
    {
        protected override T CreateEffect()
        {
            var gameObjectFromPool = base.CreateEffect();
            gameObjectFromPool.gameObject.SetActive(false);
            
            return base.CreateEffect();
        }

        protected override void OnGetElement(T effect)
        {
            effect.gameObject.SetActive(true);
        }

        protected override void OnRelease(T effect)
        {
            effect.gameObject.SetActive(false);
        }

        protected override void OnDestroyFromPool(T effect)
        {
            Destroy(effect.gameObject);
        }

        public override T Get(Vector3 position)
        {
            var gameObjectFromPool = base.Get(position);
            gameObjectFromPool.transform.position = position;

            return gameObjectFromPool;
        }
    }
}