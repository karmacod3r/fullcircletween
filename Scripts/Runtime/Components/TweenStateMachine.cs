using System.Collections.Generic;
using System.Linq;
using FullCircleTween.Core;
using FullCircleTween.Extensions;
using FullCircleTween.Properties;
using UnityEngine;

namespace FullCircleTween.Components
{
    public class TweenStateMachine : MonoBehaviour
    {
        public bool controlledByParent = true;
        [SerializeField] private string currentState = "Visible";
        [SerializeField] private List<TweenState> tweenStates = new()
        {
            new() { stateName = "Hidden" },
            new() { stateName = "Visible" }
        };
        public string[] StateNames => tweenStates?.Select(groupClip => groupClip.stateName).ToArray();

        public string CurrentState
        {
            get => currentState;
            set
            {
                if (currentState == value) return;
                currentState = value;
                OnStateChanged();
            }
        }

        private void OnStateChanged()
        {
            ApplyState(currentState);
        }

        private void ApplyState(string stateName)
        {
            var newState = TryGetState(stateName);
            if (newState == null) return;
            
            KillAll();
            newState.Play(this);
            PropagateStateChangeToChildren(transform, newState.delay);
        }

        private void PropagateStateChangeToChildren(Transform root, float delay)
        {
            if (root == null) return;

            this.DelayedCall(delay, () =>
            {
                var tweenPlayers = root.GetComponents<TweenStateMachine>();

                var stateApplied = false;
                foreach (var tweenPlayer in tweenPlayers)
                {
                    if (tweenPlayer == null || tweenPlayer == this || !tweenPlayer.controlledByParent || !tweenPlayer.HasState(currentState)) continue;

                    tweenPlayer.CurrentState = currentState;
                    stateApplied = true;
                }

                if (!stateApplied)
                {
                    foreach (Transform t in root)
                    {
                        PropagateStateChangeToChildren(t, 0);
                    }
                }
            });
        }

        private int GetStateIndex(string stateName) => tweenStates.FindIndex(clipGroup => clipGroup.stateName == stateName);

        public bool HasState(string stateName) => GetStateIndex(stateName) > -1;
        
        public TweenState GetState(string stateName)
        {
            var index = GetStateIndex(stateName);
            return index > -1 ? tweenStates[index] : null;
        }

        public void Add(string stateName, TweenGroupClip groupClip)
        {
            if (groupClip == null) return;
            if (HasState(stateName))
            {
                Debug.LogError($"TweenPlayer already contains a tween group for state '{stateName}'");
                return;
            }

            tweenStates.Add(new TweenState
                {
                    stateName = stateName,
                    tweenGroup = groupClip
                }
            );
        }

        public void Remove(string stateName)
        {
            var index = GetStateIndex(stateName);
            if (index == -1) return;

            tweenStates[index].tweenGroup.Pause();
            tweenStates.RemoveAt(index);
        }

        private TweenGroupClip TryGetState(string stateName)
        {
            var index = GetStateIndex(stateName);
            if (index == -1)
            {
                return null;
            }

            return tweenStates[index].tweenGroup;
        }

        private void Play(string stateName)
        {
            TryGetState(stateName)?.Play(this);
        }

        public void KillAll()
        {
            tweenStates.ForEach(state => state.tweenGroup.Kill());
            this.KillAllTweens();
        }

        public void Pause(string stateName)
        {
            TryGetState(stateName)?.Pause();
        }

        private int stateIndex;
    }
}