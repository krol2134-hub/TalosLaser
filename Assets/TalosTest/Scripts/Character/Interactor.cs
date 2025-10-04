using System.Collections.Generic;
using TalosTest.Interactables;
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
        [SerializeField] private LayerMask connecntionLayer;
        [SerializeField] private float placeDistance = 2;

        private const string DropTooltip = "Drop";
        private const string ConnectTooltip = "Connect";
        
        private readonly List<LaserInteractable> _heldConnections = new();
        
        private MovableTool _heldTool;

        public Transform CameraTransform => cameraTransform;
        public float PlaceDistance => placeDistance;
        public IReadOnlyCollection<LaserInteractable> HeldConnections => _heldConnections;

        public void PickUpTool(MovableTool tool)
        {
            if (tool == null)
            {
                Debug.LogError($"{name}: try pick up tool with NULL reference");
                return;
            }

            _heldTool = tool;

            Instantiate(_heldTool.PlacedTool, heldItemRoot);

            _heldTool.PickUp(this);

            ClearHeldConnections();
        }

        public void DropTool()
        {
            if (_heldTool is not null)
            {
                _heldTool.Drop(this);
            }

            _heldTool = null;

            var childCount = heldItemRoot.transform.childCount;
            for (var i = childCount - 1; i >= 0; i--)
            {
                Destroy(heldItemRoot.transform.GetChild(i).gameObject);
            }

            ClearHeldConnections();
        }

        private void ClearHeldConnections()
        {
            foreach (var heldConnection in _heldConnections)
            {
                heldConnection.UpdateSelectVisual(false);
            }

            _heldConnections.Clear();
        }

        public void HandleInteractInput()
        {
            if (_heldTool is null)
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
                if (TryGetLaserInteractable(out var connectionInteractable))
                {
                    connectionInteractable.UpdateSelectVisual(true);
                    _heldConnections.Add(connectionInteractable);
                    return;
                }

                DropTool();
            }
        }

        public string GetInteractText()
        {
            if (_heldTool is null)
            {
                return GetLookingAt<ITool>(interactableLayer, interactDistance)?.GetPickUpText();
            }
            else
            {
                
                if (TryGetLaserInteractable(out var laserInteractable))
                {
                    return laserInteractable.GetSelectText();
                }
                
                return _heldConnections.Count > 0 ? ConnectTooltip : DropTooltip;
            }
        }

        private bool TryGetLaserInteractable(out LaserInteractable connectionPoint)
        {
            connectionPoint = default;
            
            var connection = GetLookingAt<LaserInteractable>(connecntionLayer, connectDistance);
            if (connection is null)
            {
                return false;
            }
                
            var isNewConnection = !_heldConnections.Contains(connection);
            if (isNewConnection)
            {
                connectionPoint = connection;
                return true;
            }

            return false;
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