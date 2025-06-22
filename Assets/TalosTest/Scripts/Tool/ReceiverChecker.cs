using System;
using UnityEngine;
using UnityEngine.Events;

namespace TalosTest.Tool
{
    public class ReceiverChecker : MonoBehaviour
    {
        [SerializeField] private Receiver[] receivers;
        [SerializeField] private UnityEvent onAllReceiverActivated;
        
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
            foreach (var receiver in receivers)
            {
                if (!receiver.IsActivate)
                {
                    return;
                }
            }
            
            onAllReceiverActivated?.Invoke();
        }
    }
}