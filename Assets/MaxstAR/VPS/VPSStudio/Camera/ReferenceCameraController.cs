/*==============================================================================
Copyright 2017 Maxst, Inc. All Rights Reserved.
==============================================================================*/

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using System.IO;
using maxstAR;



public class ReferenceCameraController : MonoBehaviour
{
    public GameObject cameraPrefab;

    private Camera currentCamera = null;
    private GameObject currentCameraImage = null;
    private GameObject cameraGameObject;

    public void Clear()
    {
        if (cameraGameObject == null)
        {
            Transform findTransfrom = gameObject.transform.Find("ReferenceCameras");
            if (findTransfrom != null)
            {
                cameraGameObject = findTransfrom.gameObject;
            }
            else
            {
                return;
            }
        }

        Clear(cameraGameObject);

        DestroyImmediate(cameraGameObject);
        cameraGameObject = null;
    }

    public void Clear(GameObject topObject)
    {
        foreach (Transform child in topObject.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        if(topObject.transform.childCount > 0)
        {
            Clear(topObject);
        }
    }

    public void MakeCameras(Vector3 target = new Vector3(), bool loadAll = true)
    {
        Clear();

        if (!VPSLoader.Instance.IsLoaded())
        {
            VPSLoader.Instance.Load();
        }

        cameraGameObject = new GameObject();
        cameraGameObject.name = "ReferenceCameras";
        cameraGameObject.transform.parent = gameObject.transform;
        cameraGameObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        VPSUnityCamera[] vPSUnityCameas;

        if (loadAll)
        {
            vPSUnityCameas = VPSLoader.Instance.GetVPSUnityCameras();
        }
        else
        {
            vPSUnityCameas = VPSLoader.Instance.GetVPSUnityCameras(target);
        }

        Dictionary<string, GameObject> cameraGroups = new Dictionary<string, GameObject>();

        GameObject currentGroup = null;
        for (int i = 0; i < vPSUnityCameas.Length; i++)
        {
            VPSUnityCamera eachUnityCamera = vPSUnityCameas[i];


            Matrix4x4 m = eachUnityCamera.viewMatrix;
            Vector3 originailPosition = MatrixUtils.PositionFromMatrix(m);
            Quaternion originalRotation = MatrixUtils.QuaternionFromMatrix(m);


            Quaternion rotation = originalRotation;
            Vector3 position = originailPosition;
            Vector3 _mVpsPosition = new Vector3(position.x, -position.y, position.z);
            Vector3 eulerRotation = rotation.eulerAngles;
            eulerRotation.x = -eulerRotation.x;
            eulerRotation.z = -eulerRotation.z;

            _mVpsPosition = Quaternion.Euler(90, 0, 0) * _mVpsPosition;
            rotation = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z);

            position = Quaternion.Euler(180, 0, 0) * _mVpsPosition;
            rotation = Quaternion.Euler(180, 0, 0) * rotation;

            string ImageFileName = eachUnityCamera.imageFileName;
            string[] splitedImageFileName = ImageFileName.Split('_');
            string lastRemovedFileName = "";

            for(int j = 0; j < splitedImageFileName.Length-1; j++)
            {
                lastRemovedFileName = lastRemovedFileName + "_" + splitedImageFileName[j];
            }

            if(!cameraGroups.ContainsKey(lastRemovedFileName))
            {
                GameObject eachCameraGroup = Instantiate(cameraPrefab, cameraGameObject.transform);
                eachCameraGroup.layer = 10;
                eachCameraGroup.name = "Group" + lastRemovedFileName;
                eachCameraGroup.transform.position = position;
                eachCameraGroup.transform.eulerAngles = Vector3.zero;
                eachCameraGroup.transform.localScale = new Vector3(1, 1, 1);
                cameraGroups[lastRemovedFileName] = eachCameraGroup;
                currentGroup = cameraGroups[lastRemovedFileName];
            }
            
            GameObject seperatorObject = new GameObject();
            seperatorObject.transform.parent = currentGroup.transform;
            seperatorObject.name = "ReferenceCamera" + i;

          
            VPSSelectController vPSSelectController = seperatorObject.AddComponent<VPSSelectController>();

            vPSSelectController.fy = eachUnityCamera.fy;
            vPSSelectController.worldRotation = rotation;
            vPSSelectController.worldPosition = position;
            vPSSelectController.parentCamera = currentGroup;
            vPSSelectController.cameraWidth = eachUnityCamera.width;
            vPSSelectController.cameraHeight = eachUnityCamera.height;
            vPSSelectController.cameraNumber = i;
            vPSSelectController.cameraImageName = eachUnityCamera.imageFileName;
            vPSSelectController.referenceCameraController = this;

            if(i == 5433)
            {
                vPSSelectController.makeCamera();
            }
        }
        GameObject firstVPSCamera = cameraGroups.Values.ElementAt<GameObject>(0);
        GameObject secondVPSCamera = cameraGroups.Values.ElementAt<GameObject>(1);
        float distacne = Vector3.Distance(firstVPSCamera.transform.position, secondVPSCamera.transform.position);

        float cameraScaleValue = 1.0f;
        if(distacne > 1)
        {
            cameraScaleValue = cameraScaleValue * distacne / 4;
        }
        else
        {
            cameraScaleValue = cameraScaleValue * distacne * 2;
        }

        Vector3 cameraModelScale = new Vector3(cameraScaleValue, cameraScaleValue, cameraScaleValue);
        firstVPSCamera.transform.localScale = cameraModelScale;

        for (int i = 1; i < cameraGroups.Values.Count; i++)
        {
            secondVPSCamera = cameraGroups.Values.ElementAt<GameObject>(i);
            distacne = Vector3.Distance(firstVPSCamera.transform.position, secondVPSCamera.transform.position);

            if(distacne > 2)
            {
                firstVPSCamera = secondVPSCamera;
                firstVPSCamera.transform.localScale = cameraModelScale;
            }
            else
            {
                DestroyImmediate(secondVPSCamera);
            }
        }
    }

    private void ConvertGPSTOXYZ(double[] gps, float[] xyz)
    {
        double m_gpsScale = 87789.189856;
        double m_gpsRatio = 1.263158;
        double m_offsetLat = 47.360437;
        double m_offsetLon = 127.028833;

        double lat = gps[0];
        double lon = gps[1];
        xyz[0] = (float)((lat * m_gpsRatio - m_offsetLat) * m_gpsScale);
        xyz[1] = (float)((lon - m_offsetLon) * m_gpsScale);
        xyz[2] = -4.0f;
    }

    public void SetCurrentCamera(Camera currentCamera, GameObject currentCameraImage)
    {
        this.currentCamera = currentCamera;
        this.currentCameraImage = currentCameraImage;
    }

    public void DeleteCurrentCamera()
    {
        if (this.currentCamera != null)
        {
            DestroyImmediate(this.currentCamera);
            this.currentCamera = null;

            DestroyImmediate(this.currentCameraImage);
            this.currentCameraImage = null;
        }
        else
        {
            Transform findTransfrom = null;
            Transform[] ts = gameObject.transform.GetComponentsInChildren<Transform>(true);
            foreach (var child in ts)
            {
                if (child.name == "CameraImage")
                {
                    findTransfrom = child;
                }
            }

            Camera findCamera = gameObject.GetComponentInChildren<Camera>();
            if (findTransfrom != null && findCamera != null)
            {
                this.currentCameraImage = findTransfrom.gameObject;
                this.currentCamera = findCamera;
                DeleteCurrentCamera();
            }
            else
            {
                return;
            }
        }
    }

    public void OnReload()
    {
        Transform findTransfrom = gameObject.transform.Find("ReferenceCamera");
        if (findTransfrom != null)
        {
            cameraGameObject = findTransfrom.gameObject;
        }

        if(cameraGameObject != null)
        {
            VPSSelectController[] child = cameraGameObject.GetComponentsInChildren<VPSSelectController>();
            foreach (VPSSelectController each in child)
            {
                if (each.gameObject.name == "ReferenceCamera5433")
                {
                    each.makeCamera();
                }
            }
        }
    }
}