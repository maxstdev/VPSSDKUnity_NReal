using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using maxstAR;
using NRKernal;

public class NRealSceneManager : MonoBehaviour
{
    private AndroidEngine androidEngine;
    private NRCollectYUV nRCollectYUV;
    public GameObject arCamera;
    private VPSStudioController vPSStudioController = null;

    public List<GameObject> disableObjects = new List<GameObject>();
    public GameObject rootTrackable;
    public List<GameObject> occlusionObjects = new List<GameObject>();

    public Material buildingMaterial;
    public Material runtimeBuildingMaterial;

    private string serverName = "";

    private void Awake()
    {
        androidEngine = new AndroidEngine();

        vPSStudioController = FindObjectOfType<VPSStudioController>();
        if (vPSStudioController == null)
        {
            Debug.LogError("Can't find VPSStudioController. You need to add VPSStudio prefab in scene.");
            return;
        }
        else
        {
            string tempServerName = vPSStudioController.vpsServerName;
            serverName = tempServerName;
            vPSStudioController.gameObject.SetActive(false);
        }

        if (rootTrackable == null)
        {
            Debug.LogError("You need to add Root Trackable.");
        }

        foreach (GameObject eachObject in disableObjects)
        {
            if (eachObject != null)
            {
                eachObject.SetActive(false);
            }
        }
    }
    void Start()
    {

        foreach (GameObject eachGameObject in occlusionObjects)
        {
            Renderer[] cullingRenderer = eachGameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer eachRenderer in cullingRenderer)
            {
                eachRenderer.material.renderQueue = 1900;
                eachRenderer.material = runtimeBuildingMaterial;
            }
        }

        nRCollectYUV = new NRCollectYUV();
        nRCollectYUV.Play();
        TrackerManager.GetInstance().StartTracker();

        if (serverName != "")
        {
            string vpsquery = "{\"vps_server\":\"" + serverName + "\"}";
            TrackerManager.GetInstance().AddTrackerData(vpsquery);
        }
    }

    void Update()
    {
        TrackerManager.GetInstance().UpdateFrame();
        nRCollectYUV.UpdateFrame();
        var eyePoseFromHead = NRFrame.EyePoseFromHead;
        Matrix4x4 Mhe = MatrixUtils.ConvertPoseToMatrix4x4(eyePoseFromHead.RGBEyePos);

        ARFrame arFrame = TrackerManager.GetInstance().GetARFrame();

        if (arFrame.GetARLocationRecognitionState() == ARLocationRecognitionState.ARLocationRecognitionStateNormal)
        {
            Matrix4x4 targetPose = arFrame.GetTransform(Mhe);

            arCamera.transform.position = MatrixUtils.PositionFromMatrix(targetPose);
            arCamera.transform.rotation = MatrixUtils.QuaternionFromMatrix(targetPose);
            arCamera.transform.localScale = MatrixUtils.ScaleFromMatrix(targetPose);

            rootTrackable.SetActive(true);
        }
        else
        {
            rootTrackable.SetActive(false);
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            TrackerManager.GetInstance().StopTracker();
            nRCollectYUV.Stop();
        }
        else
        {
            nRCollectYUV.Play();
            TrackerManager.GetInstance().StartTracker();
            if (serverName != "")
            {
                string vpsquery = "{\"vps_server\":\"" + serverName + "\"}";
                TrackerManager.GetInstance().AddTrackerData(vpsquery);
            }
        }
    }

    void OnDestroy()
    {
        nRCollectYUV.Stop();
        TrackerManager.GetInstance().StopTracker();
        TrackerManager.GetInstance().DestroyTracker();
    }
}
