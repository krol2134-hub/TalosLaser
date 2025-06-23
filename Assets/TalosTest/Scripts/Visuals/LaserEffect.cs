using UnityEngine;

namespace TalosTest.Visuals
{
    // based on Hovl Studio laser script
    [RequireComponent(typeof(LineRenderer))]
    public class LaserEffect : MonoBehaviour
    {
        [SerializeField] private float maxLength = 30;
        [SerializeField] private float mainTextureLength = 1f;
        [SerializeField] private float noiseTextureLength = 1f;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Noise = Shader.PropertyToID("_Noise");
        
        private LineRenderer _lineRenderer;

        private Vector4 _length = new(1, 1, 1, 1);

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            _lineRenderer.material.SetTextureScale(MainTex, new Vector2(_length[0], _length[1]));
            _lineRenderer.material.SetTextureScale(Noise, new Vector2(_length[2], _length[3]));
        }

        public void Clear()
        {
            _lineRenderer.positionCount = 1;
            _lineRenderer.SetPosition(0, transform.position);
        }

        public void AddPoint(Vector3 point)
        {
            _lineRenderer.positionCount++;
            _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, point);
        }
    }
}