using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace ITintouch.MagicLeap
{
    /// <summary>
    /// This class handles video recording and loading based on controller
    /// input.
    /// </summary>
    ///
    [DefaultExecutionOrder(-10)]
    public class CVCameraCapture : MonoBehaviour
    {
        [SerializeField, Tooltip("Desired width for the camera capture")]
        private int captureWidth = 1280;

        [SerializeField, Tooltip("Desired height for the camera capture")]
        private int captureHeight = 720;

        private bool isCameraConnected;
        private MLCamera.StreamCapability selectedCapability;

        private MLCamera colorCamera;
        private bool cameraDeviceAvailable;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private float currentAspectRatio;

        private Texture2D renderTexture;
        private Matrix4x4 cameraTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, 1));

        public bool IsCapturing { get; private set; }
        public bool IsPrepared { get; private set; }
        public int CaptureWidth => captureWidth;
        public int CaptureHeight => captureHeight;
        public int CaptureFrameRate => 30;
        public Texture2D RenderTexture => renderTexture;
        public Matrix4x4 CameraTransform => cameraTransform;

        /// <summary>
        /// Using Awake so that Permissions is set before PermissionRequester Start.
        /// </summary>
        void Awake()
        {
            IsCapturing = true;

            Debug.Log("Requesting camera permission");

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
        }

        /// <summary>
        /// Stop the camera, unregister callbacks, and stop input and permissions APIs.
        /// </summary>
        void OnDisable()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            if (colorCamera != null && isCameraConnected)
            {
                DisableMLCamera();
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            cameraTransform = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        }
#endif

        /// <summary>
        /// Captures a still image using the device's camera and returns
        /// the data path where it is saved.
        /// </summary>
        /// <param name="fileName">The name of the file to be saved to.</param>
        public void StartVideoCapture()
        {
            if (!isCameraConnected)
            {
                IsCapturing = true;
                return;
            }

            if (IsCapturing) return;

            IsPrepared = false;

            Debug.Log("Starting video capture");

            MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.RGBA_8888;
            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig();
            captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS;
            captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
            captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create(selectedCapability, outputFormat);
            MLResult result = colorCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);
            if (result.IsOk)
            {
                result = colorCamera.PreCaptureAEAWB();
                result = colorCamera.CaptureVideoStart();
                if (!result.IsOk)
                {
                    Debug.LogError("Failed to start video capture!");
                }
            }

            IsCapturing = result.IsOk;
        }

        public void StopVideoCapture()
        {
            if (IsCapturing)
            {
                colorCamera?.CaptureVideoStop();
            }

            IsCapturing = false;
            IsPrepared = false;
        }

        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private IEnumerator EnableMLCamera()
        {
            if (colorCamera != null)
            {
                yield return null;
            }

            Debug.Log("Checking camera availability...");

            while (!cameraDeviceAvailable)
            {
                MLResult result =
                    MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.CV, out cameraDeviceAvailable);
                if (!(result.IsOk && cameraDeviceAvailable))
                {
                    // Wait until camera device is available
                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("Camera device available");
            yield return new WaitForSeconds(1.0f);

            MLCamera.ConnectContext context = MLCamera.ConnectContext.Create();
            context.EnableVideoStabilization = true;
            context.CamId = MLCamera.Identifier.CV;

            colorCamera = MLCamera.CreateAndConnect(context);
            if (colorCamera != null)
            {
                Debug.Log("Camera device connected");
                isCameraConnected = true;
                MLCamera.StreamCapability[] streamCapabilities =
                    MLCamera.GetImageStreamCapabilitiesForCamera(colorCamera, MLCamera.CaptureType.Video);
                if (streamCapabilities == null || streamCapabilities.Length <= 0)
                {
                    Debug.LogError("Camera device unable to received stream caps.");
                    yield break;
                }

                if (!MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth,
                        captureHeight,
                        MLCamera.CaptureType.Video, out selectedCapability))
                {
                    Debug.LogError("Camera device unable to fit stream caps to chosen options.");
                    yield break;
                }

                Debug.Log("Camera device received stream caps");
                colorCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;

                if (IsCapturing)
                {
                    IsCapturing = false;
                    StartVideoCapture();
                }
            }
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// </summary>
        private void DisableMLCamera()
        {
            if (colorCamera != null)
            {
                colorCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
                colorCamera.Disconnect();
                // Explicitly set to false here as the disconnect was attempted.
                isCameraConnected = false;
                colorCamera = null;
            }
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="imageData">The raw data of the image.</param>
        private void OnCaptureRawVideoFrameAvailable(MLCamera.CameraOutput capturedFrame,
            MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
        {
            MLCamera.FlipFrameVertically(ref capturedFrame);
            UpdateRGBTexture(ref renderTexture, capturedFrame.Planes[0]);

            Intrinsics = resultExtras.Intrinsics.Value;

#if !UNITY_EDITOR
            if (MLCVCamera.GetFramePose(resultExtras.VCamTimestamp, out cameraTransform) != MLResult.Code.Ok)
            {
                cameraTransform = Matrix4x4.identity;
            }
#endif
        }

        public MLCamera.IntrinsicCalibrationParameters Intrinsics { get; private set; }

        private void OnPermissionDenied(string permission)
        {
            Debug.LogError($"{permission} denied, example won't function.");
        }

        private void OnPermissionGranted(string permission)
        {
            Debug.Log("CVCamera permission granted");
            StartCoroutine(EnableMLCamera());
        }

        private void UpdateRGBTexture(ref Texture2D videoTextureRGB, MLCamera.PlaneInfo imagePlane)
        {
            int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);

            if (videoTextureRGB != null &&
                (videoTextureRGB.width != imagePlane.Width || videoTextureRGB.height != imagePlane.Height))
            {
                Destroy(videoTextureRGB);
                videoTextureRGB = null;
            }

            if (videoTextureRGB == null)
            {
                videoTextureRGB = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32,
                    false);
                videoTextureRGB.filterMode = FilterMode.Bilinear;
            }

            if (imagePlane.Stride != actualWidth)
            {
                var newTextureChannel = new byte[actualWidth * imagePlane.Height];
                for (int i = 0; i < imagePlane.Height; i++)
                {
                    Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel, i * actualWidth,
                        actualWidth);
                }

                videoTextureRGB.LoadRawTextureData(newTextureChannel);
            }
            else
            {
                videoTextureRGB.LoadRawTextureData(imagePlane.Data);
            }

            videoTextureRGB.Apply();
            IsPrepared = actualWidth > 0;
        }
    }
}