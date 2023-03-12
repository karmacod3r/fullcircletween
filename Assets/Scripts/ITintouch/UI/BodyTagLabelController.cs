using FullCircleData;
using FullCircleData.Attributes;
using FullCircleData.Properties;
using TMPro;
using UnityEngine;

namespace ITintouch.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class BodyTagLabelController : BestBehaviour
    {
        [GetComponent] private TMP_Text label;
        
        [Binding(nameof(BodyTagController.labelText), nameof(LabelTextChanged))]
        private Observer<string> labelText;

        private void LabelTextChanged()
        {
            label.text = labelText.Value;
        }
    }
}