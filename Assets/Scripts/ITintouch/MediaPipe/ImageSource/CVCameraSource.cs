using System;
using System.Collections;
using ITintouch.MagicLeap;
using UnityEngine;

namespace ITintouch.MediaPipe.ImageSource
{
    public class CVCameraSource : ImageSource
    {
        [SerializeField] private CVCameraCapture cvCameraCapture;

        public override string sourceName => "CVCamera";

        public override string[] sourceCandidateNames => new[] { sourceName };

        public override ResolutionStruct[] availableResolutions => cvCameraCapture == null
            ? null
            : new[]
            {
                new ResolutionStruct(cvCameraCapture.CaptureWidth, cvCameraCapture.CaptureHeight,
                    cvCameraCapture.CaptureFrameRate)
            };

        public override bool isPlaying => cvCameraCapture.IsCapturing && cvCameraCapture.IsPrepared;
        public override bool isPrepared => cvCameraCapture.IsPrepared;

        public override int textureWidth => cvCameraCapture.CaptureWidth;
        public override int textureHeight => cvCameraCapture.CaptureHeight;

        public override void SelectSource(int sourceId) { }

        private void Awake()
        {
            Debug.Log(GetType().Name + ".Awake");
            
            if (cvCameraCapture == null)
            {
                throw new ArgumentException("Please provide a reference to a CvCameraCapture");
            }
        }

        public override IEnumerator Play()
        {
            cvCameraCapture.StartVideoCapture();
            while (!cvCameraCapture.IsPrepared)
            {
                yield return null;
            }
        }

        public override IEnumerator Resume()
        {
            cvCameraCapture.StopVideoCapture();
            yield return null;
        }

        public override void Pause()
        {
            cvCameraCapture.StopVideoCapture();
        }

        public override void Stop()
        {
            cvCameraCapture.StopVideoCapture();
        }

        public override Texture GetCurrentTexture()
        {
            return cvCameraCapture.RenderTexture;
        }
    }
}