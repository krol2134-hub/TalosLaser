using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Character
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform heldItemRoot;
        [SerializeField] private float interactDistance = 2;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float placeDistance = 2;

        public MovableTool HeldTool { get; private set; }

        public Transform CameraTransform => cameraTransform;
        public float PlaceDistance => placeDistance;

        public void PickUpTool(MovableTool tool)
        {
            HeldTool = tool;

            var childCount = heldItemRoot.transform.childCount;
            for (var i = childCount - 1; i >= 0; i--)
            {
                Destroy(heldItemRoot.transform.GetChild(i).gameObject);
            }

            if (tool is not null)
            {
                Instantiate(tool.PlacedTool, heldItemRoot);
            }
        }

        public void HandleInteractInput()
        {
            if (HeldTool is null)
            {
                GetLookingAt(interactableLayer, interactDistance)?.Interact(this);
            }
            else
            {
                HeldTool.InteractWithToolInHands(this);
            }
        }

        public string GetInteractText()
        {
            if (HeldTool is null)
            {
                return GetLookingAt(interactableLayer, interactDistance)?.GetInteractText(this);
            }
            else
            {
                return HeldTool.GetInteractWithToolInHandsText(this);
            }
        }

        private IInteractable GetLookingAt(LayerMask layerMask, float maxDistance)
        {
            var isHitTool = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, maxDistance, layerMask);
            if (isHitTool)
            {
                return hit.collider.GetComponentInParent<IInteractable>();
            }

            return null;
        }
    }
}