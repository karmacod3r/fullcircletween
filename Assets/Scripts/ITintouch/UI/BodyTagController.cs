using FullCircleData;
using FullCircleData.Attributes;
using FullCircleData.Properties;
using FullCircleTween.Components;
using ITintouch.MediaPipe;
using UnityEngine;

namespace ITintouch.UI
{
    public class BodyTagController : BestBehaviour, IDataSource
    {
        public Observable<string> labelText;
        public Observable<LandmarkWeight[]> weights; 

        [GetComponentInParent] private DepthLandmarkListProjector projector;
        [GetComponent] private TweenStateMachine stateMachine;
        
        private void LateUpdate()
        {
            Render();
        }

        private void Render()
        {
            if (!Application.isPlaying) return;
            
            Vector3 position;
            var valid = projector.BlendPosition(weights.Value, out position);
            stateMachine.CurrentState = valid ? "Visible" : "Hidden";

            transform.position = position;
        }
    }
}
