using UnityEngine;

namespace TalosTest
{
    public class TetrominoAnimation : MonoBehaviour
    {

        public GameObject MeshObject;
        public float RotationSpeed = 1f;
        public float TranslationAmplitude = 0.5f;
        public float TranslationFrequency = 2f;

        private void Update()
        {
            MeshObject.transform.localRotation = Quaternion.Euler(0.0f, (Time.time * RotationSpeed * 360f) % 360f, 0.0f);
            MeshObject.transform.localPosition = new Vector3(0.0f, Mathf.Sin(Time.time * TranslationFrequency) * TranslationAmplitude, 0.0f);   
        }
    }
}