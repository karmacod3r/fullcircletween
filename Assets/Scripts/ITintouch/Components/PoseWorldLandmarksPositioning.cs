using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using ITintouch.MagicLeap;
using ITintouch.MediaPipe;
using Mediapipe;
using Mediapipe.Unity.PoseTracking;
using UnityEngine;

namespace ITintouch.Components
{
    public class PoseWorldLandmarksPositioning : MonoBehaviour
    {
        private const int LeftHipIndex = 23;
        private const int RightHipIndex = 24;

        [SerializeField] private PoseTrackingSolution poseTrackingSolution;
        [SerializeField] private CVCameraCapture cvCameraCapture;
        [SerializeField]private DepthCameraCapture depthCameraCapture;
        [SerializeField]private Transform targetTransform;
        [SerializeField]private Transform childTransform;
        [SerializeField]private Vector2 depthCoordsScale = new Vector2(1f, 1f);
        [SerializeField]private float depthValueScale = 4f;
        [SerializeField]private float depthOffset = 1f;
        [SerializeField]private bool updateTransform = true;
        
        private Vector3 offset;
        private NormalizedLandmarkList poseLandmarks;

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
            // TODO: this is a quick and dirty approximation that doesn't account for camera distortion
            if (poseLandmarks == null || poseLandmarks.Landmark == null) return;
            if (depthCameraCapture == null || cvCameraCapture == null) return;

            var hipCenter = CalculateHipCenter(poseLandmarks.Landmark);

            var depth = GetDepth(
                depthCameraCapture.ImageTexture,
                Mathf.RoundToInt(hipCenter.x * cvCameraCapture.CaptureWidth * depthCoordsScale.x),
                Mathf.RoundToInt(hipCenter.y * cvCameraCapture.CaptureHeight * depthCoordsScale.y),
                depthCameraCapture.CaptureWidth,
                depthCameraCapture.CaptureHeight
            ) * depthValueScale + depthOffset;

            // calculate offset
            var frustumHeight = 2.0f * depth * Mathf.Tan(cvCameraCapture.Intrinsics.FOV * 0.5f * Mathf.Deg2Rad);
            var scale = frustumHeight;
            offset = new Vector3(hipCenter.x * scale, -hipCenter.y * scale, depth);

            // set transform
            if (updateTransform)
            {
                var cameraTransform = cvCameraCapture.CameraTransform;
                targetTransform.position = cameraTransform.GetPosition();
                targetTransform.rotation = cameraTransform.rotation;

                childTransform.localPosition = offset;
            }
            else
            {
                targetTransform.position = Vector3.zero;
                targetTransform.rotation = Quaternion.identity;

                childTransform.localPosition = Vector3.zero;
            }
        }

        private int Wrap(int value, int size)
        {
            var ret = value % size;
            return ret < 0 ? size + ret : size;
        }

        private float GetDepth(Texture2D texture, int x, int y, int bufferWidth, int bufferHeight)
        {
            if (texture == null) return 0;

            return texture.GetPixel(Wrap(x, bufferWidth), Wrap(bufferHeight - 1 - y, bufferHeight)).r;
        }

        /// <summary>
        /// calculates the 2d center position of the hip in a range of [-0.5, 0.5]
        /// </summary>
        /// <param name="landmarks"></param>
        /// <returns></returns>
        private Vector2 CalculateHipCenter(IList<NormalizedLandmark> landmarks)
        {
            var leftHip = landmarks[LeftHipIndex];
            var rightHip = landmarks[RightHipIndex];

            if (leftHip == null || rightHip == null)
            {
                Debug.LogError("Hip landmarks are undefined");
                return Vector2.zero;
            }

            return new Vector2(
                (leftHip.X + rightHip.X) * 0.5f - 0.5f,
                (leftHip.Y + rightHip.Y) * 0.5f - 0.5f
            );
        }
    }
}