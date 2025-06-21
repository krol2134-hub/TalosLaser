using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public abstract class MovableTool : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject placedTool;
        [SerializeField] private BoxCollider placedToolCollider;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private LayerMask placeObstaclesMask;

        private const float DownCastDistance = 1000f;
        
        public GameObject PlacedTool => placedTool;

        public abstract string GetInteractText();
        public abstract string GetInteractWithToolInHandsText();

        public virtual void Interact(Interactor interactor)
        {
            PickUp(interactor);
        }

        public virtual void PickUp(Interactor interactor)
        {
            var isHeldFree = interactor.HeldTool is null;
            if (!isHeldFree)
            {
                return;
            }
            
            interactor.PickUpTool(this);
            transform.position = Vector3.zero;
            placedTool.SetActive(false);
        }

        public virtual void Drop(Interactor interactor)
        {
            var isSameHeldTool = interactor.HeldTool == this;
            if (!isSameHeldTool)
            {
                return;
            }
            
            interactor.DropTool();
            transform.position = GetPlacePosition(interactor);
            transform.rotation = Quaternion.identity;
            placedTool.SetActive(true);
        }

        private Vector3 GetPlacePosition(Interactor interactor)
        {
            var halfExtents = placedToolCollider.size * 0.5f;
            var offset = placedToolCollider.center;
            var forwardDistance = interactor.PlaceDistance;
            
            var isHitObstacleToTool = Physics.BoxCast(interactor.CameraTransform.position + offset, halfExtents, interactor.CameraTransform.forward, out var forwardHit, Quaternion.identity, forwardDistance, placeObstaclesMask);
            if (isHitObstacleToTool)
            {
                forwardDistance = forwardHit.distance;
            }
            
            var forwardPos = interactor.CameraTransform.position + interactor.CameraTransform.forward * (forwardDistance - 0.01f);
            var downDistance = DownCastDistance;

            var isHitObstacleByInteractorView = Physics.BoxCast(forwardPos + offset, halfExtents, Vector3.down, out var downHit, Quaternion.identity, downDistance, placeObstaclesMask);
            if (isHitObstacleByInteractorView)
            {
                downDistance = downHit.distance - offset.y;
            }
            
            return forwardPos + Vector3.down * downDistance - offset;
        }

    }
}