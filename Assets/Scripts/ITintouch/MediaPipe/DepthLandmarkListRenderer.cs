using System.Collections.Generic;
using Mediapipe;
using UnityEngine;

namespace ITintouch.MediaPipe
{
    public class DepthLandmarkListRenderer : MonoBehaviour
    {
        private const float VisibilityThreshold = 0.01f;

        [SerializeField] private DepthLandmarkListProjector depthLandmarkListProjector;
        [SerializeField] private GameObject landmarkPrefab;

        private List<SpriteRenderer> landmarkRenderers = new();

        private void LateUpdate()
        {
            if (depthLandmarkListProjector.LandmarkPositions == null
                || depthLandmarkListProjector.Landmarks == null) return;

            RenderLandmarks(depthLandmarkListProjector.Landmarks, depthLandmarkListProjector.LandmarkPositions);
        }

        private void RenderLandmarks(NormalizedLandmarkList landmarks, Vector3[] positions)
        {
            AdjustChildCount(positions.Length, landmarkPrefab);

            for (var i = 0; i < positions.Length; i++)
            {
                var landmark = landmarks.Landmark[i];
                var child = landmarkRenderers[i];
                var position = positions[i];

                if (landmark.Visibility < VisibilityThreshold)
                {
                    child.enabled = false;
                    return;
                }

                child.enabled = true;
                child.transform.parent.position = position;
            }
        }

        private void AdjustChildCount(int count, GameObject prefab)
        {
            for (var i = landmarkRenderers.Count; i < count; i++)
            {
                var go = Instantiate(prefab, transform);
                landmarkRenderers.Add(go.GetComponentInChildren<SpriteRenderer>());
            }

            for (var i = landmarkRenderers.Count; i > count; i--)
            {
                landmarkRenderers.RemoveAt(i - 1);

                var child = transform.GetChild(i - 1);
                if (child == null) continue;

                Destroy(child.gameObject);
            }
        }
    }
}