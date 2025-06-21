using System.Collections.Generic;
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

        private MovableTool _heldTool;
        private readonly List<IConnectionPoint> _heldConnections = new();

        public Transform CameraTransform => cameraTransform;
        public float PlaceDistance => placeDistance;
        public IReadOnlyCollection<IConnectionPoint> HeldConnections => _heldConnections;

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
            
            _heldConnections.Clear();
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
                if (TryGetConnectPoint(out var connectionPoint))
                {
                    _heldConnections.Add(connectionPoint);
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
                
                if (TryGetConnectPoint(out var connectionPoint))
                {
                    return connectionPoint.GetSelectText();
                }
                
                return _heldTool.GetInteractWithToolInHandsText();
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

        private bool TryGetConnectPoint(out IConnectionPoint connectionPoint)
        {
            connectionPoint = default;
            
            var connection = GetLookingAt<IConnectionPoint>(connecntionLayer, connectDistance);
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
    }
}