using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VPSSelectController))]
public class VPSSelectControllerEditor : Editor
{
    void OnEnable()
    {
        if(!Application.isPlaying)
        {
            VPSSelectController vpsCameraController = (VPSSelectController)target;
            if (vpsCameraController.gameObject.activeSelf)
            {
                vpsCameraController.makeCamera();
            }
        }
    }
}
