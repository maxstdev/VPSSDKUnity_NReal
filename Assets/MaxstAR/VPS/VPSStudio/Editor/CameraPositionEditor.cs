using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraPositionController))]
public class CameraPositionEditor : Editor
{
    public string serverName = "";
    public override void OnInspectorGUI()
    {
        CameraPositionController cameraPositionController = (CameraPositionController)target;

        cameraPositionController.serverName = EditorGUILayout.TextField("Object Name: ", cameraPositionController.serverName);
        EditorGUILayout.Separator();
        GUIContent makeContent = new GUIContent("Make");
        if (GUILayout.Button(makeContent, GUILayout.MaxWidth(Screen.width), GUILayout.MaxHeight(50)))
        {
            cameraPositionController.Make();
        }
        GUILayout.Space(10);

        GUIContent stopContent = new GUIContent("Stop");
        if (GUILayout.Button(stopContent, GUILayout.MaxWidth(Screen.width), GUILayout.MaxHeight(50)))
        {
            cameraPositionController.Stop();
        }
        GUILayout.Space(10);

        GUIContent cleanContent = new GUIContent("Clean");
        if (GUILayout.Button(cleanContent, GUILayout.MaxWidth(Screen.width), GUILayout.MaxHeight(50)))
        {
            cameraPositionController.Clean();
        }
    }

}
