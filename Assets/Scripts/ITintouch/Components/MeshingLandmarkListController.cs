using System.Collections.Generic;
using ITintouch.MagicLeap;
using ITintouch.MediaPipe;
using Mediapipe;
using Mediapipe.Unity.PoseTracking;
using UnityEngine;

namespace ITintouch.Components
{
    public class MeshingLandmarkListController : MonoBehaviour
    {
        private const float VisibilityThreshold = 0.5f;

        [SerializeField] private PoseTrackingSolution poseTrackingSolution;
        [SerializeField] private CVCameraCapture cvCameraCapture;
        [SerializeField] private GameObject landmarkPrefab;

        [SerializeField] private float cameraOriginZ = 0f;
        [SerializeField] private float cameraPlaneDepth = 2f;

        private Vector3 offset;
        private NormalizedLandmarkList poseLandmarks;
        private List<Renderer> landmarkRenderers = new();
        
        private void OnEnable()
        {
            poseTrackingSolution.receivedNormalizedLandmarks += SetPoseLandmarks;
        }

        private void OnDisable()
        {
            poseTrackingSolution.receivedNormalizedLandmarks -= SetPoseLandmarks;
        }

        private void SetPoseLandmarks(NormalizedLandmarkList value)
        {
            poseLandmarks = value;
        }

        private void LateUpdate()
        {
            if (poseLandmarks == null || poseLandmarks.Landmark == null) return;
            if (cvCameraCapture == null) return;

            RenderLandmarks(poseLandmarks.Landmark);
        }

        private void RenderLandmarks(IList<NormalizedLandmark> landmarks)
        {
            AdjustChildCount(landmarks.Count, landmarkPrefab);

            var cameraTransform = cvCameraCapture.CameraTransform;
            transform.position = cameraTransform.GetPosition();
            transform.rotation = cameraTransform.rotation;

            for (int i = 0; i < landmarks.Count; i++)
            {
                var landmark = landmarks[i];
                var child = landmarkRenderers[i];

                Render(child, landmark);
            }
        }

        private void Render(Renderer child, NormalizedLandmark landmark)
        {
            if (landmark.Visibility < VisibilityThreshold)
            {
                child.enabled = false;
                return;
            }

            child.enabled = true;

            var x = landmark.X - 0.5f;
            var y = -(landmark.Y - 0.5f) * cvCameraCapture.CaptureHeight / cvCameraCapture.CaptureWidth;

            var cameraOrigin = transform.TransformPoint(new Vector3(0, 0, cameraOriginZ));
            var screenPosition = transform.TransformPoint(new Vector3(x, y, cameraPlaneDepth - cameraOriginZ));
            var rayDirection = screenPosition - cameraOrigin;
            rayDirection.Normalize();
            
            RaycastHit hitInfo;
            if (!Physics.Raycast(cameraOrigin, rayDirection, out hitInfo))
            {
                child.enabled = false;
                return;
            }

            child.transform.position = hitInfo.point;
        }

        private void AdjustChildCount(int count, GameObject prefab)
        {
            for (var i = landmarkRenderers.Count; i < count; i++)
            {
                var go = Instantiate(prefab, transform);
                landmarkRenderers.Add(go.GetComponent<Renderer>());
            }

            for (var i = landmarkRenderers.Count; i > count; i--)
            {
                landmarkRenderers.RemoveAt(i - 1);

                var child = transform.GetChild(i - 1);
                if (child == null) continue;

                Destroy(child.gameObject);
            }
        }

        private int Wrap(int value, int size)
        {
            var ret = value % size;
            return ret < 0 ? size + ret : size;
        }
    }
}