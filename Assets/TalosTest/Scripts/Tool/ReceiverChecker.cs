using System;
using UnityEngine;
using UnityEngine.Events;

namespace TalosTest.Tool
{
    public class ReceiverChecker : MonoBehaviour
    {
        [SerializeField] private Receiver[] receivers;
        [SerializeField] private UnityEvent onAllReceiverActivated;
        [SerializeField] private UnityEvent onDeactivated;

        private bool _currentActivateState;
        
        private void OnEnable()
        {
            foreach (var receiver in receivers)
            {
                receiver.OnStateChanged += ReceiverStateChangedHandler;
            }
        }

        private void OnDisable()
        {
            foreach (var receiver in receivers)
            {
                receiver.OnStateChanged -= ReceiverStateChangedHandler;
            }
        }

        private void ReceiverStateChangedHandler()
        {
            var activateState = true;
            foreach (var receiver in receivers)
            {
                if (!receiver.IsActivate)
                {
                    activateState = false;
                    break;
                }
            }

            if (_currentActivateState == activateState)
            {
                return;
            }

            _currentActivateState = activateState;

            if (_currentActivateState)
            {
                onAllReceiverActivated?.Invoke();
            }
            else
            {
                onDeactivated?.Invoke();
            }
        }
    }
}