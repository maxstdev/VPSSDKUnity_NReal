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
    private GameObject rootTrackable;

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

        if (rootTrackable == null)
        {
            Debug.LogError("Can't find TrackingObject. You need to add TrackingObject in VPStudioController.");
            return;
        }

        ClearMesh();

        EditorCoroutineUtility.StartCoroutine(LoadAssetResource(vpsPath, vpsName, rootTrackable), this);
    }

    public IEnumerator LoadAssetResource(string path, string vpsName, GameObject attachObject)
    {
        string destinationfolderPath = Application.streamingAssetsPath + "/../MaxstAR/Contents/" + vpsName;
        if (!Directory.Exists(destinationfolderPath))
        {
            Directory.CreateDirectory(destinationfolderPath);
        }

        string fbx_url = path + "/" + vpsName + ".fbx";
        string fbx_url_dest = destinationfolderPath + "/" + vpsName + ".fbx";
        string fbxmeta_url = path + "/" + vpsName + ".fbx.meta";
        string fbxmeta_url_dest = destinationfolderPath + "/" + vpsName + ".fbx.meta";
        string prefab_url = path + "/" + vpsName + ".prefab";
        string prefab_url_dest = destinationfolderPath + "/" + vpsName + ".prefab";
        string prefab_meta_url = path + "/" + vpsName + ".prefab.meta";
        string prefab_meta_url_dest = destinationfolderPath + "/" + vpsName + ".prefab.meta";
        System.IO.File.Copy(fbx_url, fbx_url_dest, true);
        System.IO.File.Copy(fbxmeta_url, fbxmeta_url_dest, true);
        System.IO.File.Copy(prefab_url, prefab_url_dest, true);
        System.IO.File.Copy(prefab_meta_url, prefab_meta_url_dest, true);

        yield return new WaitForEndOfFrame();

        AssetDatabase.Refresh();

        meshObject = PrefabUtility.LoadPrefabContents(destinationfolderPath + "/" + vpsName + ".prefab");
        meshObject.transform.parent = attachObject.transform;
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

        string destinationfolderPath = Application.streamingAssetsPath + "/../MaxstAR/Contents/" + vpsName;
        if (Directory.Exists(destinationfolderPath))
        {
            Directory.Delete(destinationfolderPath, true);
            if (File.Exists(destinationfolderPath + ".meta"))
            {
                File.Delete(destinationfolderPath + ".meta");
            }
            
        }

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
