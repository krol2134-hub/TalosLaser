using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Visuals
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserEffect : MonoBehaviour
    {
        [SerializeField] private float maxLength = 30;
        [SerializeField] private float mainTextureLength = 1f;
        [SerializeField] private float noiseTextureLength = 1f;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Noise = Shader.PropertyToID("_Noise");
        
        private Vector4 _length = new(1, 1, 1, 1);

        public LineRenderer LineRenderer { get; private set; }

        public ColorType ColorType { get; private set; }

        private void Awake()
        {
            LineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            LineRenderer.sharedMaterial.SetTextureScale(MainTex, new Vector2(_length[0], _length[1]));
            LineRenderer.sharedMaterial.SetTextureScale(Noise, new Vector2(_length[2], _length[3]));
        }

        public void Setup(ColorType colorType)
        {
            ColorType = colorType;
        }

        public void Clear()
        {
            LineRenderer.positionCount = 1;
            LineRenderer.SetPosition(0, transform.position);
        }

        public void AddPoint(Vector3 point)
        {
            LineRenderer.positionCount++;
            LineRenderer.SetPosition(LineRenderer.positionCount - 1, point);
        }
    }
}