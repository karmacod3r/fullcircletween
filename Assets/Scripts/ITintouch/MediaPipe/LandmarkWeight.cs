using System;
using ITintouch.Utils;

namespace ITintouch.MediaPipe
{
    [Serializable]
    public struct LandmarkWeight
    {
        public LandmarkType landmark;
        public float weight;
    }
}