using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public abstract class MovableTool : MonoBehaviour, IInteractable
    {

        public GameObject PlacedTool;
        public BoxCollider PlacedToolCollider;
        public GameObject FirstPersonVisualsPrefab;
        public LayerMask PlaceObstaclesMask;


        public virtual void Interact(Interactor interactor)
        {
            PickUp(interactor);
        }

        public virtual void InteractWithToolInHands(Interactor interactor)
        {
            Place(interactor);
        }

        public abstract string GetInteractText(Interactor interactor);
        public abstract string GetInteractWithToolInHandsText(Interactor interactor);

        protected void PickUp(Interactor interactor)
        {
            if (interactor.HeldTool is null)
            {
                interactor.PickUpTool(this);
                transform.position = Vector3.zero;
                PlacedTool.SetActive(false);
            }
        }

        protected void Place(Interactor interactor)
        {
            if (interactor.HeldTool == this)
            {
                interactor.PickUpTool(null);
                transform.position = GetPlacePosition(interactor);
                transform.rotation = Quaternion.identity;
                PlacedTool.SetActive(true);
            }
        }

        public Vector3 GetPlacePosition(Interactor interactor)
        {
            Vector3 halfExtents = PlacedToolCollider.size * 0.5f;
            Vector3 offset = PlacedToolCollider.center;
            
            float forwardDistance = interactor.PlaceDistance;
            if (Physics.BoxCast(interactor.CameraTransform.position + offset, halfExtents,
                    interactor.CameraTransform.forward, out var forwardHit,
                    Quaternion.identity, forwardDistance, PlaceObstaclesMask))
            {
                forwardDistance = forwardHit.distance;
            }
            Vector3 forwardPos = interactor.CameraTransform.position + interactor.CameraTransform.forward * (forwardDistance - 0.01f);
            float downDistance = 1000f;
            if (Physics.BoxCast(forwardPos + offset, halfExtents, Vector3.down, out var downHit, 
                    Quaternion.identity, downDistance, PlaceObstaclesMask))
            {
                downDistance = downHit.distance - offset.y;
            }
            
            return forwardPos + Vector3.down * downDistance - offset;
        }

    }
}