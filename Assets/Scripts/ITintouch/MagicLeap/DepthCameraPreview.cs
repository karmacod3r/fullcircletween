using UnityEngine;
using UnityEngine.UI;

namespace ITintouch.MagicLeap
{
    [RequireComponent(typeof(RawImage))]
    public class DepthCameraPreview : MonoBehaviour
    {
        private RawImage rawImage;
        
        [SerializeField] 
        private DepthCameraCapture depthCameraCapture;

        private Texture2D texture;
        private Color[] pixels;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            rawImage.texture = depthCameraCapture.ImageTexture;
        }
    }
}
