using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Character
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform heldItemRoot;
        [SerializeField] private float interactDistance = 2;
        [SerializeField] private float connectDistance = 20;
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
                GetLookingAt<IInteractable>(interactableLayer, interactDistance)?.Interact(this);
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
                return GetLookingAt<IInteractable>(interactableLayer, interactDistance)?.GetInteractText(this);
            }
            else
            {
                var generator = GetLookingAt<IGenerator>(interactableLayer, connectDistance);
                if (generator is not null)
                {
                    return generator.GetConnectText();
                }
                
                return HeldTool.GetInteractWithToolInHandsText(this);
            }
        }
        
        private T GetLookingAt<T>(LayerMask layerMask, float maxDistance) 
            where T : class
        {
            var isHitTool = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, maxDistance, layerMask);
            if (isHitTool)
            {
                return hit.collider.GetComponentInParent<T>();
            }

            return null;
        }
    }
}