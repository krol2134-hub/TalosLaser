using UnityEngine;

namespace TalosTest.Pools
{
    public class GameObjectPool : PoolBase<GameObject>
    {
        protected override GameObject CreateEffect()
        {
            var gameObjectFromPool = base.CreateEffect();
            gameObjectFromPool.SetActive(false);
            
            return base.CreateEffect();
        }

        protected override void OnGetElement(GameObject effect)
        {
            effect.SetActive(true);
        }

        protected override void OnRelease(GameObject effect)
        {
            effect.SetActive(false);
        }

        protected override void OnDestroyFromPool(GameObject effect)
        {
            Destroy(effect);
        }

        public override GameObject Get(Vector3 position)
        {
            var gameObjectFromPool = base.Get(position);
            gameObjectFromPool.transform.position = position;

            return gameObjectFromPool;
        }
    }
}