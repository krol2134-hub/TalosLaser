using UnityEngine;

namespace TalosTest.Visuals
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserPathEffect : MonoBehaviour
    {
        [SerializeField] private float defaultLength = 30f;
        
        private LineRenderer _lineRenderer;

        private Vector3 _endPosition;
        private float _length;
        
        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            
            SetPath(transform.forward, defaultLength);
        }

        public void SetPath(Vector3 direction, float length)
        {
            _length = length;
            _endPosition = transform.position + direction * _length;
            
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, _endPosition);
            _lineRenderer.textureScale = new Vector2(_length, 1f);
        }
    }
}