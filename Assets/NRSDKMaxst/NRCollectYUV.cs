using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;
using System;
using System.IO;
using UnityEngine.UI;
using maxstAR;

public class NRCollectYUV : NRRGBCamTextureYUV
{
    private bool _isSave = false;
    private DateTime _timeStamp;
    private string _saveFolderPath;

    private Matrix4x4 _rgbEyeToHeadPose;
    private Vector4 _rgbIntrinsic;
    private bool isFirst = true;

    private float[] localPose = new float[16];
    private float[] tempPose = new float[16];
    private ulong timestamp;

    public NRCollectYUV()
    {
        
    }

    protected override void OnRawDataUpdate(FrameRawData rgbRawDataFrame)
    {
        //base.OnRawDataUpdate(rgbRawDataFrame);

        if (isFirst)
        {
            int imageWidth = Width;
            int imageHeight = Height;
            int scale = 1;

            if (imageWidth % 640 == 0)
                scale = imageWidth / 640;
            else if (imageWidth % 720 == 0)
                scale = imageWidth / 720;
            else
                scale = 1;

            NativeMat3f intrinsic = NRFrame.GetRGBCameraIntrinsicMatrix();
            float fx = intrinsic.column0.X / scale;
            float fy = intrinsic.column1.Y / scale;
            float px = intrinsic.column2.X / scale;
            float py = intrinsic.column2.Y / scale;

            int resized_imageWidth = (int)imageWidth / scale;
            int resized_imageHeight = (int)imageHeight / scale;

            CameraDeviceInternal.GetInstance().SetFusionCameraIntrinsic(intrinsic.column0.X, intrinsic.column1.Y, intrinsic.column2.X, intrinsic.column2.Y);
            CameraDeviceInternal.GetInstance().SetCalibrationData(resized_imageWidth, resized_imageHeight, fx, fy, px, py);

            isFirst = false;
        }
        timestamp = rgbRawDataFrame.timeStamp;

        Pose head_pose = Pose.identity;
        bool result = NRFrame.GetHeadPoseByTime(ref head_pose, timestamp);
        

        Pose eyeToHeadRgbPose = NRDevice.Instance.NativeHMD.GetEyePoseFromHead((int)NativeEye.RGB);
        this._rgbEyeToHeadPose = ConvertPoseToMatrix4x4(eyeToHeadRgbPose);

        if (result)
        {
            Matrix4x4 Mwh = ConvertPoseToMatrix4x4(head_pose);
            Matrix4x4 Mhe = this._rgbEyeToHeadPose;

            Matrix4x4 Mrl = GetLeft2RightHandedMatrix();

            Matrix4x4 Mwel = Mwh * Mhe; // Left-handed Mwe
            Matrix4x4 Mwer = Mrl * Mwel * Mrl; // Right-handed Mwe

            localPose[0] = Mwer.m00;
            localPose[1] = Mwer.m10;
            localPose[2] = Mwer.m20;
            localPose[3] = Mwer.m30;

            localPose[4] = Mwer.m01;
            localPose[5] = Mwer.m11;
            localPose[6] = Mwer.m21;
            localPose[7] = Mwer.m31;

            localPose[8] = Mwer.m02;
            localPose[9] = Mwer.m12;
            localPose[10] = Mwer.m22;
            localPose[11] = Mwer.m32;

            localPose[12] = Mwer.m03;
            localPose[13] = Mwer.m13;
            localPose[14] = Mwer.m23;
            localPose[15] = Mwer.m33;

            CameraDeviceInternal.GetInstance().SetNewVPSCameraPoseAndTimestamp(localPose, timestamp);

            if (CameraDeviceInternal.GetInstance().RequestAsyncImage())
            {
                CameraDeviceInternal.GetInstance().SetNewFrameAndPoseAndTimestamp(rgbRawDataFrame.data, rgbRawDataFrame.data.Length, Width, Height, ColorFormat.YUV420, localPose, timestamp);
                CameraDeviceInternal.GetInstance().SetAsyncImage(false);
            }
        }
    }

    public void UpdateFrame()
    {
        Pose head_pose = Pose.identity;
        bool result = NRFrame.GetHeadPoseByTime(ref head_pose, 0);

        Pose eyeToHeadRgbPose = NRDevice.Instance.NativeHMD.GetEyePoseFromHead((int)NativeEye.RGB);
        this._rgbEyeToHeadPose = ConvertPoseToMatrix4x4(eyeToHeadRgbPose);
        if (result)
        {
            Matrix4x4 Mwh = ConvertPoseToMatrix4x4(head_pose);
            Matrix4x4 Mhe = this._rgbEyeToHeadPose;

            Matrix4x4 Mrl = GetLeft2RightHandedMatrix();

            Matrix4x4 Mwel = Mwh * Mhe; // Left-handed Mwe
            Matrix4x4 Mwer = Mrl * Mwel * Mrl; // Right-handed Mwe


            tempPose[0] = Mwer.m00;
            tempPose[1] = Mwer.m10;
            tempPose[2] = Mwer.m20;
            tempPose[3] = Mwer.m30;

            tempPose[4] = Mwer.m01;
            tempPose[5] = Mwer.m11;
            tempPose[6] = Mwer.m21;
            tempPose[7] = Mwer.m31;

            tempPose[8] = Mwer.m02;
            tempPose[9] = Mwer.m12;
            tempPose[10] = Mwer.m22;
            tempPose[11] = Mwer.m32;

            tempPose[12] = Mwer.m03;
            tempPose[13] = Mwer.m13;
            tempPose[14] = Mwer.m23;
            tempPose[15] = Mwer.m33;

            CameraDeviceInternal.GetInstance().SetSyncCameraFrameAndPoseAndTimestamp(null, 0, tempPose, timestamp);
        }
        
    }


    private static Matrix4x4 GetLeft2RightHandedMatrix()
    {
        Matrix4x4 Mc = Matrix4x4.identity;
        Mc.m22 = -Mc.m22;
        return Mc;
    }

    public Matrix4x4 ConvertPoseToMatrix4x4(Pose pose)
    {
        Matrix4x4 result = Matrix4x4.Rotate(pose.rotation);
        result.m03 = pose.position.x;
        result.m13 = pose.position.y;
        result.m23 = pose.position.z;

        return result;
    }
}