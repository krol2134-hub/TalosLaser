using UnityEngine;

namespace TalosTest.Visuals
{
    public class TetrominoAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject animationTarget;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float translationAmplitude = 0.5f;
        [SerializeField] private float translationFrequency = 2f;

        private void Update()
        {
            Animate();
        }

        private void Animate()
        {
            animationTarget.transform.localRotation = Quaternion.Euler(0.0f, (Time.time * rotationSpeed * 360f) % 360f, 0.0f);
            animationTarget.transform.localPosition = new Vector3(0.0f, Mathf.Sin(Time.time * translationFrequency) * translationAmplitude, 0.0f);
        }
    }
}