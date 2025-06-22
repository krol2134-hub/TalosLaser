using System;
using System.Collections.Generic;
using TalosTest.Character;
using UnityEngine;

namespace TalosTest.Tool
{
    public class Connector : MovableTool, IConnectionPoint
    {
        [SerializeField] private Transform laserPoint;
        
        private readonly List<IConnectionPoint> _connections = new();
        
        public override string GetPickUpText()
        {
            return "Take Connector";
        }

        public override string GetInteractWithToolInHandsText()
        {
            return "Drop";
        }

        public override void PickUp(Interactor interactor)
        {
            base.PickUp(interactor);
            
            _connections.Clear();
        }

        public override void Drop(Interactor interactor)
        {
            base.Drop(interactor);
            
            //TODO Is not safe, cause in up code can clear it before drop. Need to rework by throw connections in arguments instead property
            _connections.AddRange(interactor.HeldConnections);
        }

        public string GetSelectText()
        {
            return "Select";
        }

        public Transform GetPoint()
        {
            return laserPoint;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            for (var index = 0; index < _connections.Count; index++)
            {
                var connectorPosition = laserPoint.position;
                var nextConnection = _connections[index];
                
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(connectorPosition, nextConnection.GetPoint().position);
            }
        }
#endif
    }
}