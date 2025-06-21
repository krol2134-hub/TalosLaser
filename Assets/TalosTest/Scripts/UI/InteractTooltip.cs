using JetBrains.Annotations;
using TalosTest.Character;
using TMPro;
using UnityEngine;

namespace TalosTest
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class InteractTooltip : MonoBehaviour
    {
        
        private TextMeshProUGUI text;
        
        [CanBeNull] private Interactor interactor;

        private void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            interactor = FindObjectOfType<Interactor>();
        }

        private void Update()
        {
            text.text = interactor is null ? "" : interactor.GetInteractText();
        }
    }
}