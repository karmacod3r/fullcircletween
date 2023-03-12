using System;
using System.Collections.Generic;
using ITintouch.MagicLeap;
using ITintouch.Utils;
using Mediapipe;
using UnityEngine;
using UnityEngine.Assertions;

namespace ITintouch.MediaPipe
{
    public class DepthLandmarkListProjector : MonoBehaviour
    {
        [SerializeField] private PoseTrackingSolution poseTrackingSolution;
        [SerializeField] private CVCameraCapture cvCameraCapture;
        [SerializeField] private DepthCameraCapture depthCameraCapture;
        [SerializeField] private Vector2 depthCoordsScale = new Vector2(1f, 1f);
        [SerializeField] private float depthValueScale = 4f;
        [SerializeField] private float depthOffset = 1f;
        [SerializeField] private float offsetScale = 0.5f;
        [SerializeField] private float depthSmoothing = 1f;

        private NormalizedLandmarkList poseLandmarks;

        private float[] depthBuffer = Array.Empty<float>();
        private Vector3[] landmarkPositions = Array.Empty<Vector3>();
        private float lastTimer;

        public NormalizedLandmarkList Landmarks => poseLandmarks;
        public Vector3[] LandmarkPositions => landmarkPositions;

        public bool GetPosition(LandmarkType landmarkType, out Vector3 position, float minVisibility = 0.1f)
        {
            var index = (int)landmarkType;

            if (Landmarks == null || index >= landmarkPositions.Length || index < 0 || index >= landmarkPositions.Length)
            {
                position = Vector3.zero;
                return false;
            }
            
            var landmark = Landmarks.Landmark[(int)landmarkType];
            if (landmark.Visibility < minVisibility)
            {
                position = Vector3.zero;
                return false;
            } 
            
            position = landmarkPositions[index];
            return true;
        }
        
        public bool BlendPosition(LandmarkWeight[] weights, out Vector3 position)
        {
            var validLandmarks = 0;
            position = Vector3.zero;

            foreach (var weight in weights)
            {
                Vector3 v;
                if (! GetPosition(weight.landmark, out v)) continue;

                position += v * weight.weight;
                validLandmarks++;
            }

            return validLandmarks > 0;
        }

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
            // TODO: this is a quick and dirty approximation that doesn't account for camera intrinsics / distortion
            if (poseLandmarks == null || poseLandmarks.Landmark == null) return;
            if (depthCameraCapture == null || cvCameraCapture == null) return;

            CalculateLandmarkPositions(poseLandmarks.Landmark);
        }

        private void CalculateLandmarkPositions(IList<NormalizedLandmark> landmarks)
        {
            var dt = Time.time - lastTimer;
            lastTimer = Time.time;
            
            if (landmarks.Count != depthBuffer.Length)
            {
                Array.Resize(ref depthBuffer, landmarks.Count);
                Array.Resize(ref landmarkPositions, landmarks.Count);
            }

            var cameraTransform = cvCameraCapture.CameraTransform;
            transform.position = cameraTransform.GetPosition();
            transform.rotation = cameraTransform.rotation;

            for (int i = 0; i < landmarks.Count; i++)
            {
                var landmark = landmarks[i];
                CalculatePosition(landmark, dt, ref depthBuffer[i], ref landmarkPositions[i]);
            }
        }
        
        private void CalculatePosition(NormalizedLandmark landmark, float dt, ref float depth, ref Vector3 position)
        {
            var x = landmark.X - 0.5f;
            var y = (landmark.Y - 0.5f) * cvCameraCapture.CaptureHeight / cvCameraCapture.CaptureWidth;

            var depthMeters = depthCameraCapture.ImageTexture.GetPixel(
                Mathf.RoundToInt(x * depthCameraCapture.CaptureWidth * depthCoordsScale.x) +
                depthCameraCapture.CaptureWidth / 2,
                Mathf.RoundToInt(y * depthCameraCapture.CaptureHeight * depthCoordsScale.y) +
                depthCameraCapture.CaptureHeight / 2
            ).r;
            var currentDepth = depthMeters * depthValueScale + depthOffset;

            var s = Mathf.Abs(dt * 1000f * depthSmoothing) + 1f;
            depth = Mathf.Lerp(depth, currentDepth, Mathf.Clamp01(1f / s));

            // TODO: use cv camera intrinsics
            // calculate offset
            var fov = Application.isEditor ? 60 : depthCameraCapture.fov;
            var frustumHeight = 2.0f * depth * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            var scale = frustumHeight * offsetScale;
            position = transform.TransformPoint(new Vector3(x * scale, -y * scale, depth));
        }
    }
}