using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System;
using System.Runtime.InteropServices;
using maxstAR;

public struct VPSPoint3d
{
    public Int32 pid;
    public float x, y, z;
    public float nx, ny;
    public Int32 r, g, b;
    public Int32 category;
};

public struct VPSCamera
{
    public float x, y, z;
};

public struct VPSUnityCamera
{
    public float x, y, z;
    public float fx, fy, px, py;
    public float width, height;
    public Matrix4x4 viewMatrix;
    public Matrix4x4 projectionMatrix;
    public string imageFileName;
};

public class VPSLoader
{
    private static VPSLoader _instance = null;
    public static VPSLoader Instance
    {
        get
        {
            if (_instance == null) _instance = new VPSLoader();
            return _instance;
        }
    }

    private Dictionary<Vector2, List<VPSPoint3d>> sortedPoints = new Dictionary<Vector2, List<VPSPoint3d>>();
    private Dictionary<Vector2, List<VPSUnityCamera>> sortedCameras = new Dictionary<Vector2, List<VPSUnityCamera>>();
    private VPSPoint3d[] vpsPoints;
    private VPSCamera[] vpsCameras;
    private Matrix4x4[] vpsCameraMatrix;
    private VPSUnityCamera[] vpsUnityCameras;
    private float farClipPlane = 100.0f;
    private float nearClipPlane = 0.1f;
    private string[] imageFileNames = null;
    private bool isload = false;

    private string atn_path;
    private string atm_path;
    private string vpsPath;

    public void Clear()
    {
        sortedPoints.Clear();
        sortedCameras.Clear();
        vpsPoints = null;
        vpsCameras = null;
        vpsCameraMatrix = null;
        vpsUnityCameras = null;
        imageFileNames = null;
    }

    public bool IsLoaded()
    {
        return isload;
    }

    public string GetVPSPath()
    {
        return vpsPath;
    }

    public void SetVPSPath(string path)
    {
        this.vpsPath = path;
    }

    public void LoadFile(string vpsPath)
    {
        string folderPath = vpsPath;
        atn_path = folderPath + "/medium.atn";
        SetId2ImageName(atn_path);

        atm_path = folderPath + "/medium.atm";
    }

    public void Load()
    {
        LoadFile(vpsPath);

        if(!File.Exists(atm_path))
        {
            return;
        }
        FileStream fileStream = File.OpenRead(atm_path);

        int pointSize = 0;
        byte[] pointSizeArray = BitConverter.GetBytes(pointSize);
        fileStream.Read(pointSizeArray, 0, pointSizeArray.Length);
        pointSize = BitConverter.ToInt32(pointSizeArray, 0);

        if(pointSize != 0)
        {
            vpsPoints = new VPSPoint3d[pointSize];

            byte[] vpsPointByte = StructToByte(vpsPoints[0]);
            for (int i = 0; i < vpsPoints.Length; i++)
            {
                fileStream.Read(vpsPointByte, 0, vpsPointByte.Length);
                vpsPoints[i] = ByteToStruct<VPSPoint3d>(vpsPointByte);
            }
            SortPoint(vpsPoints);
        }
        

        int cameraSize = 0;
        byte[] cameraSizeArray = BitConverter.GetBytes(cameraSize);
        fileStream.Read(cameraSizeArray, 0, cameraSizeArray.Length);
        cameraSize = BitConverter.ToInt32(cameraSizeArray, 0);

        vpsCameras = new VPSCamera[cameraSize];
        vpsUnityCameras = new VPSUnityCamera[cameraSize];

        imageFileNames = GetImageFileNames();

        byte[] vpsCameraByte = StructToByte(vpsCameras[0]);
        for (int i = 0; i < vpsCameras.Length; i++)
        {
            fileStream.Read(vpsCameraByte, 0, vpsCameraByte.Length);
            vpsCameras[i] = ByteToStruct<VPSCamera>(vpsCameraByte);
            vpsUnityCameras[i] = new VPSUnityCamera();
            vpsUnityCameras[i].x = vpsCameras[i].x;
            vpsUnityCameras[i].y = -vpsCameras[i].z;
            vpsUnityCameras[i].z = -vpsCameras[i].y;
        }

        int width = 0;
        int height = 0;
        byte[] widthArray = BitConverter.GetBytes(width);
        fileStream.Read(widthArray, 0, widthArray.Length);
        width = BitConverter.ToInt32(widthArray, 0);

        byte[] heightArray = BitConverter.GetBytes(height);
        fileStream.Read(heightArray, 0, heightArray.Length);
        height = BitConverter.ToInt32(heightArray, 0);

        float fx = 0.0f;
        float fy = 0.0f;
        float px = 0.0f;
        float py = 0.0f;

        byte[] fxArray = BitConverter.GetBytes(fx);
        fileStream.Read(fxArray, 0, fxArray.Length);
        fx = BitConverter.ToSingle(fxArray, 0);

        byte[] fyArray = BitConverter.GetBytes(fy);
        fileStream.Read(fyArray, 0, fyArray.Length);
        fy = BitConverter.ToSingle(fyArray, 0);

        byte[] pxArray = BitConverter.GetBytes(px);
        fileStream.Read(pxArray, 0, pxArray.Length);
        px = BitConverter.ToSingle(pxArray, 0);

        byte[] pyArray = BitConverter.GetBytes(py);
        fileStream.Read(pyArray, 0, pyArray.Length);
        py = BitConverter.ToSingle(pyArray, 0);

        Matrix4x4 cameraProjectionMatrix = new Matrix4x4();
        cameraProjectionMatrix.m00 = 2.0f * fx / width;
        cameraProjectionMatrix.m11 = 2.0f * -fy / height;
        cameraProjectionMatrix.m02 = (2.0f * px - width) / width;
        cameraProjectionMatrix.m12 = (2.0f * ((height - 1.0f) - py) - height) / height;
        cameraProjectionMatrix.m22 = (farClipPlane + nearClipPlane) / (farClipPlane - nearClipPlane);
        cameraProjectionMatrix.m32 = 1.0f;
        cameraProjectionMatrix.m23 = -(2.0f * farClipPlane * nearClipPlane) / (farClipPlane - nearClipPlane);

        float mem = 0.0f;
        vpsCameraMatrix = new Matrix4x4[cameraSize];
        byte[] memArray = BitConverter.GetBytes(mem);
        for (int i = 0; i < cameraSize; i++)
        {
            fileStream.Read(memArray, 0, memArray.Length);
            float mem0 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem1 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem2 = BitConverter.ToSingle(memArray, 0);
            float mem3 = 0.0f;
            Vector4 colum0 = new Vector4(mem0, mem1, mem2, mem3);

            fileStream.Read(memArray, 0, memArray.Length);
            float mem4 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem5 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem6 = BitConverter.ToSingle(memArray, 0);
            float mem7 = 0.0f;
            Vector4 colum1 = new Vector4(mem4, mem5, mem6, mem7);

            fileStream.Read(memArray, 0, memArray.Length);
            float mem8 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem9 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem10 = BitConverter.ToSingle(memArray, 0);
            float mem11 = 0.0f;
            Vector4 colum2 = new Vector4(mem8, mem9, mem10, mem11);

            fileStream.Read(memArray, 0, memArray.Length);
            float mem12 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem13 = BitConverter.ToSingle(memArray, 0);
            fileStream.Read(memArray, 0, memArray.Length);
            float mem14 = BitConverter.ToSingle(memArray, 0);
            float mem15 = 1.0f;
            Vector4 colum3 = new Vector4(mem12, mem13, mem14, mem15);

            vpsCameraMatrix[i] = new Matrix4x4(colum0, colum1, colum2, colum3);
            Matrix4x4 viewMatrix = vpsCameraMatrix[i];
            Vector3 position = MatrixUtils.PositionFromMatrix(viewMatrix);

            vpsUnityCameras[i].width = width;
            vpsUnityCameras[i].height = height;
            vpsUnityCameras[i].viewMatrix = vpsCameraMatrix[i];
            vpsUnityCameras[i].projectionMatrix = cameraProjectionMatrix;
            vpsUnityCameras[i].imageFileName = imageFileNames[i];
            vpsUnityCameras[i].fx = fx;
            vpsUnityCameras[i].fy = fy;
            vpsUnityCameras[i].px = px;
            vpsUnityCameras[i].py = py;
        }

        //SortCamera(vpsUnityCameras);
        isload = true;
    }

    private void SortPoint(VPSPoint3d[] points)
    {
        foreach (VPSPoint3d eachPoint in points)
        {
            Vector3 point = new Vector3(-eachPoint.x, eachPoint.y, eachPoint.z);
            float pointx = (float)Math.Round(point.x * 0.1);
            pointx = pointx * 10;
            float pointz = (float)Math.Round(point.z * 0.1);
            pointz = pointz * 10;
            if (sortedPoints.ContainsKey(new Vector2(pointx, pointz)))
            {
                List<VPSPoint3d> listPoint = sortedPoints[new Vector2(pointx, pointz)];
                listPoint.Add(eachPoint);
            }
            else
            {
                List<VPSPoint3d> listPoint = new List<VPSPoint3d>();
                sortedPoints[new Vector2(pointx, pointz)] = listPoint;
            }
        }
    }

    private void SortCamera(VPSUnityCamera[] cameras)
    {
        Vector3 saved_point = new Vector3(0,0,0);
        foreach (VPSUnityCamera eachPoint in cameras)
        {
            Vector3 point = new Vector3(eachPoint.x, eachPoint.y, eachPoint.z);
            if (Math.Abs(point.x - saved_point.x) > 1.0f || Math.Abs(point.z - saved_point.z) > 1.0f)
            {
                saved_point = point;
            }
            else
            {
                if (sortedCameras.ContainsKey(new Vector2(saved_point.x, saved_point.z)))
                {
                    List<VPSUnityCamera> listPoint = sortedCameras[new Vector2(saved_point.x, saved_point.z)];
                    listPoint.Add(eachPoint);
                }
                else
                {
                    List<VPSUnityCamera> listPoint = new List<VPSUnityCamera>();
                    sortedCameras[new Vector2(saved_point.x, saved_point.z)] = listPoint;
                }
            }
        }
    }

    public void SetId2ImageName(string fileName)
    {
        if(!File.Exists(fileName))
        {
            return;
        }
        var reader = new StreamReader(File.OpenRead(fileName));
        var imageSizeString = reader.ReadLine();
        int imageSize = 0;
        int.TryParse(imageSizeString, out imageSize);

        imageFileNames = new string[imageSize];
        for (int i=0; i<imageSize; i++)
        {
            string line = reader.ReadLine();
            imageFileNames[i] = line;
        }
    }

    public string[] GetImageFileNames()
    {
        return imageFileNames;
    }

    public VPSPoint3d[] GetVPSPoints(Vector2 point,int margin)
    {
        List<VPSPoint3d> points = new List<VPSPoint3d>();
        var test = sortedPoints;
        float cap = 50.0f;
        float pointx = (float)Math.Round(point.x * 0.1);
        pointx = pointx * 10;
        float pointz = (float)Math.Round(point.y * 0.1);
        pointz = pointz * 10;

        for(float x = pointx - cap; x < pointx + cap; x = x + margin)
        {
            for(float y = pointz - cap; y < pointz + cap; y = y + margin)
            {
                if(sortedPoints.ContainsKey(new Vector2(x, y))) {
                    List<VPSPoint3d> selectPoints = sortedPoints[new Vector2(x, y)];
                    points.AddRange(selectPoints.ToArray());
                }
                
            }
        }
        return points.ToArray();
    }

    public VPSPoint3d[] GetVPSPoints(int margin)
    {
        List<VPSPoint3d> points = new List<VPSPoint3d>();
        int currentCount = 0;
        while (true)
        {
            if(currentCount > vpsPoints.Length - 1)
            {
                break;
            }

            points.Add(vpsPoints[currentCount]);

            currentCount = currentCount + margin + 1;
        }
        return points.ToArray();
    }

    public VPSCamera[] GetVPSCameras()
    {
        return vpsCameras;
    }

    public VPSUnityCamera[] GetVPSUnityCameras()
    {
        return vpsUnityCameras;
    }

    public VPSUnityCamera[] GetVPSUnityCameras(int distance = 2)
    {
        List<VPSUnityCamera> points = new List<VPSUnityCamera>();

        VPSUnityCamera beforeCamera = vpsUnityCameras[0]; ;

        points.Add(beforeCamera);
        for (int i = 1; i < vpsUnityCameras.Length; i++)
        {
            VPSUnityCamera currentCamera = vpsUnityCameras[i];
            Vector3 beforeCameraPosition = new Vector3(beforeCamera.x, beforeCamera.y, beforeCamera.z);
            Vector3 currentCameraPosition = new Vector3(currentCamera.x, currentCamera.y, currentCamera.z);

            float currentDistance = Vector3.Distance(beforeCameraPosition, currentCameraPosition);

            if (currentDistance > distance || currentDistance < 0.2)
            {
                points.Add(currentCamera);
                beforeCamera = currentCamera;
            }
        }

        return points.ToArray();
    }

    public VPSUnityCamera[] GetVPSUnityCameras(Vector3 point)
    {
        List<VPSUnityCamera> points = new List<VPSUnityCamera>();

        float range_scale = 30;
        foreach (Vector2 key in sortedCameras.Keys)
        {
            if(Math.Abs(key.x - point.x) < range_scale && Math.Abs(key.y - point.z) < range_scale)
            {
                List<VPSUnityCamera> selectPoints = sortedCameras[key];
                points.AddRange(selectPoints.ToArray());
            }
        }

        return points.ToArray();
    }

    public static byte[] StructToByte(object obj)
    {
        int size = Marshal.SizeOf(obj);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    public static T ByteToStruct<T>(byte[] buffer) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (size > buffer.Length)
        {
            throw new Exception();
        }

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(buffer, 0, ptr, size);
        T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);

        return obj;

    }
}
