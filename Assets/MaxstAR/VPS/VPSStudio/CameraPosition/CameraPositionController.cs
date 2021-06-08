using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using maxstAR;
using System;
using JsonFx.Json;

public class CameraPositionController : MonoBehaviour
{
    string pathURL = "http://svn.maxst.com:50010";

    public string serverName = "";
    private bool m_stop = false;
    private Dictionary<int, int> tryNumbers = new Dictionary<int, int>();


    List<SimulateData> simulateDatas = new List<SimulateData>();
    public void MakeLocalizePath(SimulateData simulateData, string serverName, int number, Action<string> success, Action<int> fail) {

        //Convert.ToBase64String(customToByteArray);
        Dictionary<string, string> headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json"}
        };

        byte[] imageBytes = File.ReadAllBytes(simulateData.imagePath);
        float aspect = 1;
        float imageWidth = simulateData.imageWidth;
        float imageHeight = simulateData.imageHeight;

        if (imageWidth > 1280)
        {
            aspect = imageWidth / 960;
        }

        if (imageWidth > 1280)
        {
            imageWidth = 960;
            imageHeight = imageHeight / aspect;
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            Texture2D resizeTex = ScaleTexture(tex, (int)imageWidth, (int)imageHeight);
            imageBytes = resizeTex.EncodeToJPG();

        }


        string poseString = "" +
            simulateData.pose[0] + "," + simulateData.pose[1] + "," + simulateData.pose[2] + "," + simulateData.pose[3]
            + "," + simulateData.pose[4] + "," + simulateData.pose[5] + "," + simulateData.pose[6] + "," + simulateData.pose[7]
            + "," + simulateData.pose[8] + "," + simulateData.pose[9] + "," + simulateData.pose[10] + "," + simulateData.pose[11]
            + "," + simulateData.pose[12] + "," + simulateData.pose[13] + "," + simulateData.pose[14] + "," + simulateData.pose[15];

        float fx = simulateData.intrinsic[0] / aspect;
        float fy = simulateData.intrinsic[1] / aspect;
        float px = simulateData.intrinsic[2] / aspect;
        float py = simulateData.intrinsic[3] / aspect;

        string instrincString = fx + ",0.0," + px + ",0.0," + fy + "," + py + ",0.0,0.0,1.0";

        string base64ImageString = Convert.ToBase64String(imageBytes);

        Dictionary<string, string> parameters;
        if (serverName == "")
        {
            parameters = new Dictionary<string, string>()
            {
                { "uuid", "test"},
                { "Image", base64ImageString},
                { "Pose", poseString},
                { "Intrinsic", instrincString},
                { "Distortion", "0.0,0.0,0.0,0.0,0.0"},
                { "ImageWidth", ""+imageWidth},
                { "ImageHeight", ""+imageHeight},
                { "Gps", ""+simulateData.latitude + "," + simulateData.longitude}
            };
        } else
        {
            parameters = new Dictionary<string, string>()
            {
                { "uuid", "test"},
                { "localizer", serverName },
                { "Image", base64ImageString},
                { "Pose", poseString},
                { "Intrinsic", instrincString},
                { "Distortion", "0.0,0.0,0.0,0.0,0.0"},
                { "ImageWidth", ""+imageWidth},
                { "ImageHeight", ""+imageHeight},
                { "Gps", ""+simulateData.latitude + "," + simulateData.longitude}
            };
        }
#if UNITY_EDITOR
        EditorCoroutineUtility.StartCoroutine(APIController.POST(pathURL + "/v1/location", headers, parameters, 10, (resultString) =>
        {
            VPSData vpsData = JsonReader.Deserialize<VPSData>(resultString);
            if(vpsData.pose == null)
            {
                fail(number);
            } else
            {
                success(resultString);
            }
        }), this);
#endif
    }

    Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
        Color[] rpixels = result.GetPixels(0);
        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);
        for (int px = 0; px < rpixels.Length; px++)
        {
            rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
        }
        result.SetPixels(rpixels, 0);
        result.Apply();
        return result;
    }

    public void Make() {
        VPSStudioController vPSStudioController = FindObjectOfType<VPSStudioController>();
        if(VPSStudioController.vpsName == "")
        {
            if(vPSStudioController != null)
            {
                vPSStudioController.ReloadName();
            }
        }

        string simulatePath = vPSStudioController.vpsSimulatePath;
        
        if (Directory.Exists(simulatePath))
        {
            SimulateData[] simulateDatas = LoadSimulateData(simulatePath);

            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 0;

            SendVPS(simulateDatas, serverName, (data) =>
            {
                string[] SplitText = data.pose.Split(',');

                float[] pose = new float[16];
                pose[0] = float.Parse(SplitText[0]);
                pose[1] = float.Parse(SplitText[1]);
                pose[2] = float.Parse(SplitText[2]);
                pose[3] = float.Parse(SplitText[3]);

                pose[4] = float.Parse(SplitText[4]);
                pose[5] = float.Parse(SplitText[5]);
                pose[6] = float.Parse(SplitText[6]);
                pose[7] = float.Parse(SplitText[7]);

                pose[8] = float.Parse(SplitText[8]);
                pose[9] = float.Parse(SplitText[9]);
                pose[10] = float.Parse(SplitText[10]);
                pose[11] = float.Parse(SplitText[11]);

                pose[12] = float.Parse(SplitText[12]);
                pose[13] = float.Parse(SplitText[13]);
                pose[14] = float.Parse(SplitText[14]);
                pose[15] = float.Parse(SplitText[15]);
                float[] vpsPose = pose;

                lineRenderer.positionCount = simulateDatas.Length;
                SimulateData simulateData = simulateDatas[data.number];
                float[] fusionPose = CameraDeviceInternal.GetInstance().GetFusionPose(1, vpsPose, simulateData.pose);
 

                for (int i = 0; i < simulateDatas.Length; i++)
                {
                    SimulateData eachSimulateData = simulateDatas[i];
                    float[] convertFusionPose = CameraDeviceInternal.GetInstance().GetFusionPose(0, fusionPose, eachSimulateData.pose);
                    Matrix4x4 convertedMatrix = MatrixUtils.GetUnityPoseMatrix(convertFusionPose);

                    Matrix4x4 targetPose = convertedMatrix.inverse;

                    Quaternion rotation = Quaternion.Euler(90, 0, 0);
                    Matrix4x4 m = Matrix4x4.TRS(new Vector3(0, 0, 0), rotation, new Vector3(1, 1, 1));
                    targetPose = m * targetPose;

                    Vector3 position = MatrixUtils.PositionFromMatrix(targetPose);

                    lineRenderer.SetPosition(i, position);

                }
            }, () =>
            {
                
            });
        
            
        }
    }

    private List<PathModel> LoadFile(string folder) {
        string[] files = Directory.GetFiles(folder);

        List<PathModel> paths = new List<PathModel>();

        int i = 0;
        foreach(string eachFile in files) {
            string extention = Path.GetExtension(eachFile);
            if(extention == ".txt") {
                PathModel pathModel = LoadTxt(eachFile, i);
                paths.Add(pathModel);
                i++;
            }
        }

        return paths;
    }

    private void SendVPS(SimulateData[] simulateDatas, string serverName, Action<VPSData> complete, Action fail)
    {
        if(m_stop)
        {
            m_stop = false;
            return;
        }
        if(simulateDatas.Length == tryNumbers.Count)
        {
            fail();
            return;
        }

        if (tryNumbers.Count == 30)
        {
            fail();
            return;
        }

        int number = GetNumber(simulateDatas);
        tryNumbers[number] = number;

        SimulateData simulateData = simulateDatas[number];
        MakeLocalizePath(simulateData, serverName, number, (result) =>
        {
            VPSData vpsData = JsonReader.Deserialize<VPSData>(result);
            vpsData.number = number;
            complete(vpsData);
        }, (count) =>
        {
            Debug.Log("Fail " + count);
            SendVPS(simulateDatas, serverName, complete, fail);
        });
    }

    private int GetNumber(SimulateData[] simulateDatas)
    {
        int number = UnityEngine.Random.Range(0, simulateDatas.Length - 1);

        if(tryNumbers.ContainsKey(number))
        {
            return GetNumber(simulateDatas);
        }
        else
        {
            return number;
        }
    }
    private PathModel LoadTxt(string path, int number) {
        string text = File.ReadAllText(path);

        string[] SplitText = text.Split(',');
        PathModel pathModel = new PathModel();
        pathModel.id = ""+number;

        pathModel.x = float.Parse(SplitText[18]);
        pathModel.y = float.Parse(SplitText[19]);
        pathModel.z = float.Parse(SplitText[20]);

        pathModel.matrix[0] = float.Parse(SplitText[6]);
        pathModel.matrix[1] = float.Parse(SplitText[7]);
        pathModel.matrix[2] = float.Parse(SplitText[8]);
        pathModel.matrix[3] = float.Parse(SplitText[9]);

        pathModel.matrix[4] = float.Parse(SplitText[10]);
        pathModel.matrix[5] = float.Parse(SplitText[11]);
        pathModel.matrix[6] = float.Parse(SplitText[12]);
        pathModel.matrix[7] = float.Parse(SplitText[13]);

        pathModel.matrix[8] = float.Parse(SplitText[14]);
        pathModel.matrix[9] = float.Parse(SplitText[15]);
        pathModel.matrix[10] = float.Parse(SplitText[16]);
        pathModel.matrix[11] = float.Parse(SplitText[17]);

        pathModel.matrix[12] = float.Parse(SplitText[18]);
        pathModel.matrix[13] = float.Parse(SplitText[19]);
        pathModel.matrix[14] = float.Parse(SplitText[20]);
        pathModel.matrix[15] = float.Parse(SplitText[21]);

        return pathModel;
    }

    private SimulateData[] LoadSimulateData(string folderPath) {
        string[] files = Directory.GetFiles(folderPath);

        Dictionary<string, SimulateData> simulateDatas = new Dictionary<string, SimulateData>();

        foreach(string eachFile in files) {
            string name = Path.GetFileNameWithoutExtension(eachFile);
            string extention = Path.GetExtension(eachFile);

            SimulateData simulateData;
            if (simulateDatas.ContainsKey(name))
            {
                simulateData = simulateDatas[name];
            } else
            {
                simulateData = new SimulateData();
            }

            if(extention == ".txt") {
                string text = System.IO.File.ReadAllText(eachFile);
                string[] SplitText = text.Split(',');
                simulateData.imageWidth = float.Parse(SplitText[0]);
                simulateData.imageHeight = float.Parse(SplitText[1]);
                float[] intrinsic = new float[4];
                intrinsic[0] = float.Parse(SplitText[2]);
                intrinsic[1] = float.Parse(SplitText[3]);
                intrinsic[2] = float.Parse(SplitText[4]);
                intrinsic[3] = float.Parse(SplitText[5]);
                simulateData.intrinsic = intrinsic;

                float[] pose = new float[16];
                pose[0] = float.Parse(SplitText[6]);
                pose[1] = float.Parse(SplitText[7]);
                pose[2] = float.Parse(SplitText[8]);
                pose[3] = float.Parse(SplitText[9]);

                pose[4] = float.Parse(SplitText[10]);
                pose[5] = float.Parse(SplitText[11]);
                pose[6] = float.Parse(SplitText[12]);
                pose[7] = float.Parse(SplitText[13]);

                pose[8] = float.Parse(SplitText[14]);
                pose[9] = float.Parse(SplitText[15]);
                pose[10] = float.Parse(SplitText[16]);
                pose[11] = float.Parse(SplitText[17]);

                pose[12] = float.Parse(SplitText[18]);
                pose[13] = float.Parse(SplitText[19]);
                pose[14] = float.Parse(SplitText[20]);
                pose[15] = float.Parse(SplitText[21]);
                simulateData.pose = pose;

                double altitude = double.Parse(SplitText[22]);
                simulateData.altitude = altitude;
                double latitude = double.Parse(SplitText[23]);
                simulateData.latitude = latitude;
                double longitude = double.Parse(SplitText[24]);
                simulateData.longitude = longitude;
                simulateDatas[name] = simulateData;
            }
            if(extention == ".jpg") {
                simulateData.imagePath = eachFile;
                simulateDatas[name] = simulateData;
            }
        }

        if(simulateDatas.Count > 0)
        {
            SimulateData[] simulateArray = new SimulateData[simulateDatas.Count];
            simulateDatas.Values.CopyTo(simulateArray, 0);
            return simulateArray;
        }
        else
        {
            return null;
        }

    }

    public void Stop()
    {
        tryNumbers.Clear();
        m_stop = true;
    }
    

    public void Clean() {
        tryNumbers.Clear();
        m_stop = false;
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
    }
}
