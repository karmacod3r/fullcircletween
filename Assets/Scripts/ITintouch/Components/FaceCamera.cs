using System;
using UnityEngine;

namespace ITintouch.Components
{
    public class FaceCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);  
        }
    }
}