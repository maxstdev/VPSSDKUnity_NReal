using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(VPSStudioController))]
public class VPSStudioEditor : Editor
{
    public const string vpsPath = "/../../VPSData/VPSMap";
    public const string vpsSimulatePath = "/../../VPSData/VPSSimulationData/";
    public const string vpsServerName = "";

    private int simulate_selectIndex = 0;
    private int before_simulate_ChoiceIndex = -1;

    private int selectIndex = 0;
    private int beforeChoiceIndex = -1;

    public override void OnInspectorGUI()
    {
        VPSStudioController vpsStudioController = (VPSStudioController)target;

        EditorGUILayout.LabelField("VPS Map");
        string folderPath = Application.dataPath;
        folderPath = folderPath + vpsPath;
        string[] directories = Directory.GetDirectories(folderPath);

        List<string> directory_name = new List<string>();
        foreach (string directory in directories)
        {
            var name = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            directory_name.Add(name);
        }
        selectIndex = EditorGUILayout.Popup(vpsStudioController.SelectIndex, directory_name.ToArray());

        GUILayout.Space(10);

        EditorGUILayout.LabelField("VPS Simulation Data");
        string selectVPSName = directory_name[selectIndex];
        string[] simulate_directories = null;
        if (selectVPSName != "")
        {
            folderPath = Application.dataPath;
            folderPath = folderPath + vpsSimulatePath + selectVPSName;
            simulate_directories = Directory.GetDirectories(folderPath);

            List<string> simulate_directory_name = new List<string>();
            foreach (string directory in simulate_directories)
            {
                var name = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                simulate_directory_name.Add(name);
            }

            simulate_selectIndex = EditorGUILayout.Popup(vpsStudioController.Simulate_SelectIndex, simulate_directory_name.ToArray());
        }
        
        GUILayout.Space(10);

        EditorGUILayout.Separator();
        GUIContent makeContent = new GUIContent("Load VPS Map");
        if (GUILayout.Button(makeContent, GUILayout.MaxWidth(Screen.width), GUILayout.MaxHeight(50)))
        {
            vpsStudioController.LoadMap();
        }
        GUILayout.Space(10);

        GUIContent clearContent = new GUIContent("Clear");
        if (GUILayout.Button(clearContent, GUILayout.MaxWidth(Screen.width), GUILayout.MaxHeight(50)))
        {
            vpsStudioController.Clear();
        }
        GUILayout.Space(10);

        DrawDefaultInspector();

        bool isDirty = false;
        if (selectIndex != beforeChoiceIndex || simulate_selectIndex != before_simulate_ChoiceIndex)
        {
            isDirty = true;
            beforeChoiceIndex = selectIndex;
            vpsStudioController.SelectIndex = selectIndex;

            before_simulate_ChoiceIndex = simulate_selectIndex;
            vpsStudioController.Simulate_SelectIndex = simulate_selectIndex;
        }

        if (GUI.changed || isDirty)
        {
            vpsStudioController.vpsPath = directories[selectIndex];
            vpsStudioController.vpsServerName = directory_name[selectIndex];
            VPSStudioController.vpsName = directory_name[selectIndex];

            if(simulate_directories != null)
            {
                if(simulate_directories.Length >= simulate_selectIndex-1)
                {
                    vpsStudioController.vpsSimulatePath = simulate_directories[simulate_selectIndex];
                }
                else
                {
                    vpsStudioController.vpsSimulatePath = simulate_directories[0];
                    simulate_selectIndex = 0;
                    before_simulate_ChoiceIndex = simulate_selectIndex;
                    vpsStudioController.Simulate_SelectIndex = simulate_selectIndex;

                }
            }
            
            EditorUtility.SetDirty(target);
        }
    }
}
