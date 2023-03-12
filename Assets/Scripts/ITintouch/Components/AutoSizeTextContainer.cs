using System;
using TMPro;
using UnityEngine;

namespace ITintouch.Components
{
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class AutoSizeTextContainer : MonoBehaviour
    {
        
        public bool autoSizeTextContainer = true;
        
        private TMP_Text label;

        private void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            enabled = label != null;
            
        }

        private void Update()
        {
            label.autoSizeTextContainer = false;
            label.autoSizeTextContainer = autoSizeTextContainer;
        }
    }
}
