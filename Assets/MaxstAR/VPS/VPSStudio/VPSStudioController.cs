using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

#if UNITY_EDITOR
[InitializeOnLoadAttribute]
public static class PlayModeStateChanged
{
    // register an event handler when the class is initialized
    static PlayModeStateChanged()
    {
        EditorApplication.playModeStateChanged += VPSStudioController.PlayModeState;
    }
}
#endif
public class VPSStudioController : MonoBehaviour
{
#if UNITY_EDITOR
    public static void PlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            ReferenceCameraController referenceCameraController = FindObjectOfType<ReferenceCameraController>();
            if (referenceCameraController != null)
            {
                referenceCameraController.OnReload();
            }
        }
    }
#endif
    [HideInInspector]
    [SerializeField]
    public string vpsPath = "";

    [HideInInspector]
    [SerializeField]
    public string vpsServerName = "";

    [HideInInspector]
    [SerializeField]
    private int selectIndex;

    [HideInInspector]
    [SerializeField]
    public string vpsSimulatePath = "";

    [HideInInspector]
    [SerializeField]
    private int simulate_selectIndex;

    [HideInInspector]
    [SerializeField]
    private GameObject meshObject;

    public int SelectIndex
    {
        get
        {
            return selectIndex;
        }
        set
        {
            selectIndex = value;
        }
    }

    public int Simulate_SelectIndex
    {
        get
        {
            return simulate_selectIndex;
        }
        set
        {
            simulate_selectIndex = value;
        }
    }

    [SerializeField]
    public static string vpsName = "";

    public int GetSelectedIndex()
    {
        return selectIndex;
    }

    public void SetSelectedIndex(int index)
    {
        selectIndex = index;
    }

#if UNITY_EDITOR
    public void LoadMap()
    {
        VPSLoader.Instance.Clear();
        VPSLoader.Instance.SetVPSPath(vpsPath);
        VPSLoader.Instance.Load();

        var name = Path.GetFileName(vpsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        vpsName = name;

        ReferenceCameraController referenceCameraController = GetComponent<ReferenceCameraController>();
        referenceCameraController.Clear();
        referenceCameraController.MakeCameras();

        EditorCoroutineUtility.StartCoroutine(LoadAssetResource(vpsPath, vpsName), this);
    }

    public IEnumerator LoadAssetResource(string path, string vpsName)
    {
        string destinationfolderPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "MaxstAR" + Path.DirectorySeparatorChar + "Contents" + Path.DirectorySeparatorChar + vpsName;
        if (!Directory.Exists(destinationfolderPath))
        {
            Directory.CreateDirectory(destinationfolderPath);
        }

        string mapPath = path;
        string[] files = Directory.GetFiles(mapPath);
        string destinationFolder = destinationfolderPath;
        List<string> loadPrefabs = new List<string>();
        foreach (string file in files)
        {
            string destinationFile = "";
            string extension = Path.GetExtension(file);

            if (extension == ".fbx" || extension == ".meta" || extension == ".prefab")
            {
                destinationFile = destinationFolder + Path.DirectorySeparatorChar + Path.GetFileName(file);
                if (Path.GetFileNameWithoutExtension(destinationFile).Contains("Trackable") && Path.GetExtension(destinationFile) == ".prefab")
                {
                    loadPrefabs.Add(destinationFile);
                }
            }

            if (destinationFile != "")
            {
                System.IO.File.Copy(file, destinationFile, true);
            }
        }
      
        yield return new WaitForEndOfFrame();

        AssetDatabase.Refresh();
       

        foreach(string eachLoadFile in loadPrefabs)
        {
            GameObject local_meshObject = PrefabUtility.LoadPrefabContents(eachLoadFile);
            meshObject = Instantiate(local_meshObject);
        }
    }

    public void ClearMesh()
    {
        if (meshObject != null)
        {
            DestroyImmediate(meshObject);
        }
        else
        {
            meshObject = GameObject.Find(vpsName);
            if (meshObject)
            {
                DestroyImmediate(meshObject);
            }
        }

        //string destinationfolderPath = Application.streamingAssetsPath + "/../MaxstAR/Contents/" + vpsName;
        //if (Directory.Exists(destinationfolderPath))
        //{
        //    Directory.Delete(destinationfolderPath, true);
        //    if (File.Exists(destinationfolderPath + ".meta"))
        //    {
        //        File.Delete(destinationfolderPath + ".meta");
        //    }
        //}

        AssetDatabase.Refresh();
    }
    
    public void Clear()
    {
        ClearMesh();
        ReferenceCameraController referenceCameraController = GetComponent<ReferenceCameraController>();
        referenceCameraController.Clear();
    }
#endif

    public void ReloadName()
    {
        var name = Path.GetFileName(vpsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        vpsName = name;
    }
}
