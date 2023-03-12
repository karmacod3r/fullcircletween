using UnityEngine;
using UnityEngine.UI;

namespace ITintouch.MagicLeap
{
    public class CVCameraFollowTransform : MonoBehaviour
    {
        private RawImage rawImage;
        
        [SerializeField] 
        private CVCameraCapture cvCameraCapture;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        private void LateUpdate()
        {
            var cameraTransform = cvCameraCapture.CameraTransform;
            transform.position = cameraTransform.GetPosition();
            transform.rotation = cameraTransform.rotation;
        }
    }
}
