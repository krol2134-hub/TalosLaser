using UnityEngine;

namespace TalosTest.Interactables
{
    public class ToolTetromino : MonoBehaviour
    {

        public LayerMask PlayerLayer;
        public float collectRadius;

        private void Update()
        {
            if (Physics.CheckSphere(transform.position, collectRadius, PlayerLayer))
            {
                Destroy(gameObject);
            }
        }
    }
}