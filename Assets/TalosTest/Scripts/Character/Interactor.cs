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
        [SerializeField] private LayerMask generatorLayer;
        [SerializeField] private float placeDistance = 2;

        public MovableTool HeldTool { get; private set; }

        public Transform CameraTransform => cameraTransform;
        public float PlaceDistance => placeDistance;

        public void PickUpTool(MovableTool tool)
        {
            if (tool == null)
            {
                Debug.LogError($"{name}: try pick up tool with NULL reference");
                return;
            }
            
            HeldTool = tool;
            
            Instantiate(HeldTool.PlacedTool, heldItemRoot);
            
            HeldTool.PickUp(this);
        }
        
        public void DropTool()
        {
            if (HeldTool is not null)
            {
                HeldTool.Drop(this);
            }
            
            HeldTool = null;

            var childCount = heldItemRoot.transform.childCount;
            for (var i = childCount - 1; i >= 0; i--)
            {
                Destroy(heldItemRoot.transform.GetChild(i).gameObject);
            }
        }

        public void HandleInteractInput()
        {
            if (HeldTool is null)
            {
                var connector = GetLookingAt<ITool>(interactableLayer, interactDistance);
                //TODO Use Interface for HeldTool
                if (connector is not null)
                {
                    PickUpTool((MovableTool)connector);
                }
            }
            else
            {
                DropTool();
            }
        }

        public string GetInteractText()
        {
            if (HeldTool is null)
            {
                return GetLookingAt<ITool>(interactableLayer, interactDistance)?.GetPickUpText();
            }
            else
            {

                var generator = GetLookingAt<IGenerator>(generatorLayer, connectDistance);
                if (generator is not null)
                {
                    return generator.GetConnectText();
                }
                
                return HeldTool.GetInteractWithToolInHandsText();
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