using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VPSSelectController : MonoBehaviour
{
    public int cameraNumber = -1;
    public string cameraImageName = null;
    public float cameraWidth = 0.0f;
    public float cameraHeight = 0.0f;
    public GameObject parentCamera;
    public Vector3 worldPosition;
    public Quaternion worldRotation;
    public ReferenceCameraController referenceCameraController;

    public float fy;

    public void makeCamera()
    {
        Camera vpsCamera = GetComponent<Camera>();

        if (vpsCamera == null)
        {
            if (referenceCameraController != null)
            {
                referenceCameraController.DeleteCurrentCamera();
            }

            if (VPSStudioController.vpsName == "")
            {
                VPSStudioController vPSStudioController = FindObjectOfType<VPSStudioController>();
                if (vPSStudioController != null)
                {
                    vPSStudioController.ReloadName();
                }
            }

            if (cameraWidth == 0.0f || cameraHeight == 0.0f)
            {
                return;
            }

            Camera unityCamera = gameObject.AddComponent<Camera>();

            float vertical_fov = (2.0f * Mathf.Atan(0.5f * cameraHeight / fy)) * Mathf.Rad2Deg;
            unityCamera.fieldOfView = vertical_fov;
            unityCamera.cullingMask = 1;
            unityCamera.nearClipPlane = 0.01f;
            unityCamera.farClipPlane = 100.0f;
            unityCamera.clearFlags = CameraClearFlags.SolidColor;
            GameObject cameraImage = Resources.Load("MaxstAR/CameraImage") as GameObject;
            float fov = vertical_fov;
            GameObject instance = Instantiate<GameObject>(cameraImage);
            instance.name = "CameraImage";
            instance.transform.parent = gameObject.transform;


            instance.transform.localPosition = new Vector3(0.0f, 0.0f, fy / cameraHeight);
            instance.transform.localEulerAngles = Vector3.zero;
            float aspect = cameraWidth / cameraHeight;
            float scaleWidth = 1.0f * aspect;
            instance.transform.localScale = new Vector3(scaleWidth, 1.0f, 1.0f);

            referenceCameraController.SetCurrentCamera(unityCamera, instance);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float cameraRatio = (float)cameraWidth / cameraHeight;
            float screenRatio = (float)screenWidth / screenHeight;


            Mesh mesh = instance.gameObject.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            uvs[0].x = 0; uvs[0].y = 1;
            uvs[1].x = 1; uvs[1].y = 1;
            uvs[2].x = 0; uvs[2].y = 0;
            uvs[3].x = 1; uvs[3].y = 0;

            instance.gameObject.GetComponent<Renderer>().sharedMaterial.renderQueue = 1900;

            VPSCameraImageController vPSCameraImageController = instance.GetComponent<VPSCameraImageController>();
            vPSCameraImageController.LoadImage(cameraImageName, (result) => {
                if(cameraHeight != result.height)
                {
                    instance.transform.localEulerAngles = new Vector3(0, 0, -90);
                    instance.transform.localScale = new Vector3(1.0f, scaleWidth, 1.0f);
                }
            });

            parentCamera.transform.position = worldPosition;
            parentCamera.transform.rotation = worldRotation;

            transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        }
        else
        {
            VPSCameraImageController vPSCameraImageController = GetComponentInChildren<VPSCameraImageController>();

            GameObject cameraImage = vPSCameraImageController.gameObject;
            Texture imageTexture = cameraImage.GetComponent<Renderer>().sharedMaterial.mainTexture;

            if(imageTexture == null)
            {
                vPSCameraImageController.LoadImage(cameraImageName,(result) => {
                    if (cameraHeight != result.height)
                    {
                        float aspect = cameraWidth / cameraHeight;
                        float scaleWidth = 1.0f * aspect;
                        vPSCameraImageController.gameObject.transform.localEulerAngles = new Vector3(0, 0, -90);
                        vPSCameraImageController.gameObject.transform.localScale = new Vector3(1.0f, scaleWidth, 1.0f);
                    }
                    
                });
            }
        
        }
    }

}
