using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using System.Text;

public class ETSDK : MonoBehaviour
{
    private const string DllName = "EyeTrackingPlugin";

    #region MatDescriptor Struct
    
    // C# 版本的 MatDescriptor 结构体
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MatDescriptor
    {
        public int rows;
        public int cols;
        public int type; // OpenCV 数据类型, e.g., CV_8UC1=0, CV_8UC3=16
        public IntPtr data; // 指向Mat数据的指针
    }

    // 假定的 OpenCV 类型常量
    private const int CV_8UC1 = 0;  // 对应 TextureFormat.Alpha8
    private const int CV_8UC3 = 16; // 对应 TextureFormat.RGB24

    #endregion

    #region P/Invoke DllImport
    
    [DllImport(DllName)]
    private static extern bool ETPlugin_Init();

    [DllImport(DllName)]
    private static extern bool ETPlugin_ReloadCameraStreaming();

    [DllImport(DllName)]
    private static extern bool ETPlugin_StartStreaming();

    [DllImport(DllName)]
    private static extern bool ETPlugin_StopStreaming();

    [DllImport(DllName)]
    private static extern bool ETPlugin_GetETImages([In, Out] MatDescriptor[] outputMats, ref int count);

    [DllImport(DllName)]
    private static extern bool ETPlugin_NewDataset();

    [DllImport(DllName, CharSet = CharSet.Ansi)]
    private static extern bool ETPlugin_NewDatasetWithUserInfo(string userName);

    [DllImport(DllName)]
    private static extern bool ETPlugin_SaveImages();

    [DllImport(DllName, CharSet = CharSet.Ansi)]
    private static extern bool ETPlugin_SaveImagesAndTarget(string mode, float x, float y, float z, [In] float[] slamPoseR, [In] float[] slamPoseT, int index);

    [DllImport(DllName)]
    private static extern bool ETPlugin_StartCalibration();

    [DllImport(DllName)]
    private static extern bool ETPlugin_RecordACaliPoint(float x, float y, float z);

    [DllImport(DllName)]
    private static extern bool ETPlugin_FinishCalibration();

    [DllImport(DllName)]
    private static extern bool ETPlugin_GetTrackResult(ref bool state, [Out] float[] data);

    [DllImport(DllName)]
    private static extern bool ETPlugin_ETSDKExitAndRelease();

    [DllImport(DllName)]
    private static extern bool ETPlugin_GetExternalMatrixByETCamera(bool isNecessary);

    [DllImport(DllName, CharSet = CharSet.Ansi)]
    private static extern int ETPlugin_GetHardwareSerialNumber(StringBuilder serialNumber, int bufferSize);

    [DllImport(DllName)]
    private static extern float ETPlugin_GetCamExposure(int eye);

    [DllImport(DllName)]
    private static extern void ETPlugin_SetCamExposure(int eye, float exposure);

    [DllImport(DllName)]
    private static extern void ETPlugin_ChangeCamExposure(int eye, int flag);

    [DllImport(DllName)]
    private static extern bool ETPlugin_GetAutoAdjustCamExposureState();

    [DllImport(DllName)]
    private static extern void ETPlugin_CloseAutoAdjustCamExposure();

    [DllImport(DllName)]
    private static extern bool ETPlugin_SetMyopiaLens(int eye, int degree);

    [DllImport(DllName, CharSet = CharSet.Ansi)]
    private static extern void ETPlugin_RECORD_EVENT(string stage, long source_id, long source_id2);

    [DllImport(DllName)]
    private static extern void ETPlugin_ENABLE_TRACING(bool enabled);

    #endregion

    #region Image Buffers
    
    // 图像属性 (基于旧版 ETSDK.cs)
    private const int ET_WIDTH = 400;
    private const int ET_HEIGHT = 400;
    // 托管的 byte[] 缓冲区
    private static byte[] m_imageEt0 = null, m_imageEt1 = null;
    // 目标 Texture2D
    private static Texture2D m_textureEt0 = null, m_textureEt1 = null;

    // 非托管内存指针
    private static IntPtr m_bufferEt0 = IntPtr.Zero;
    private static IntPtr m_bufferEt1 = IntPtr.Zero;

    // MatDescriptor 数组
    private static MatDescriptor[] m_matDescriptors = null;

    private static void InitImagesBuffer()
    {
        if (m_matDescriptors == null)
        {
            // 1. 分配托管 byte 数组
            m_imageEt0 = new byte[ET_WIDTH * ET_HEIGHT];
            m_imageEt1 = new byte[ET_WIDTH * ET_HEIGHT];

            // 2. 创建 Texture2D
            m_textureEt0 = new Texture2D(ET_WIDTH, ET_HEIGHT, TextureFormat.Alpha8, false);
            m_textureEt1 = new Texture2D(ET_WIDTH, ET_HEIGHT, TextureFormat.Alpha8, false);

            // 3. 分配非托管内存
            m_bufferEt0 = Marshal.AllocHGlobal(m_imageEt0.Length);
            m_bufferEt1 = Marshal.AllocHGlobal(m_imageEt1.Length);

            // 4. 创建 MatDescriptor 数组并指向非托管内存
            m_matDescriptors = new MatDescriptor[3];
            m_matDescriptors[0] = new MatDescriptor 
            { 
                rows = ET_HEIGHT, 
                cols = ET_WIDTH, 
                type = CV_8UC1, 
                data = m_bufferEt0 
            };
            m_matDescriptors[1] = new MatDescriptor 
            { 
                rows = ET_HEIGHT, 
                cols = ET_WIDTH, 
                type = CV_8UC1, 
                data = m_bufferEt1 
            };
        }
    }

    private static void ReleaseImagesBuffer()
    {
        if (m_matDescriptors != null)
        {
            // 释放非托管内存
            Marshal.FreeHGlobal(m_bufferEt0);
            Marshal.FreeHGlobal(m_bufferEt1);

            // 重置所有引用
            m_bufferEt0 = IntPtr.Zero;
            m_bufferEt1 = IntPtr.Zero;
            m_matDescriptors = null;
            m_imageEt0 = null;
            m_imageEt1 = null;
            m_textureEt0 = null;
            m_textureEt1 = null;
        }
    }

    #endregion

    #region Public Static SDK Methods

    static public bool ET_Init()
    {
        Debug.Log("ET_Init()");
        return ETPlugin_Init();
    }

    static public bool ET_ReloadCameraStreaming()
    {
        Debug.Log("ET_ReloadCameraStreaming()");
        return ETPlugin_ReloadCameraStreaming();
    }

    static public bool ET_StartStreaming()
    {
        Debug.Log("ET_StartStreaming()");
        InitImagesBuffer(); // 在开始时初始化图像缓冲区
        return ETPlugin_StartStreaming();
    }

    static public bool ET_StopStreaming()
    {
        Debug.Log("StopStreaming()");
        return ETPlugin_StopStreaming();
    }

    static public bool ET_GetImages(out Texture2D textureEt0, out Texture2D textureEt1)
    {
        Debug.Log("ET_GetImages()");
        InitImagesBuffer(); // 确保已初始化
        
        int count = 2; // 我们期望 2 张图像
        bool flag = ETPlugin_GetETImages(m_matDescriptors, ref count);
        
        if (flag && count > 0)
        {
            try
            {
                // 1. 从非托管内存 (m_bufferX) 复制到 托管数组 (m_imageX)
                Marshal.Copy(m_bufferEt0, m_imageEt0, 0, m_imageEt0.Length);
                Marshal.Copy(m_bufferEt1, m_imageEt1, 0, m_imageEt1.Length);
                // 2. 加载到 Texture2D
                m_textureEt0.LoadRawTextureData(m_imageEt0);
                m_textureEt0.Apply();
                m_textureEt1.LoadRawTextureData(m_imageEt1);
                m_textureEt1.Apply();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ET_GetImages: Error copying image data. {ex.Message}");
                flag = false;
            }
        }
        
        textureEt0 = m_textureEt0;
        textureEt1 = m_textureEt1;
        return flag;
    }

    static public bool ET_NewDataset()
    {
        Debug.Log("ET_NewDataset()");
        return ETPlugin_NewDataset();
    }

    static public bool ET_NewDatasetWithUserInfo(string userName)
    {
        Debug.LogFormat("ET_NewDatasetWithUserInfo({0})", userName);
        return ETPlugin_NewDatasetWithUserInfo(userName);
    }

    static public bool ET_SaveImages()
    {
        Debug.Log("ET_SaveImages()");
        return ETPlugin_SaveImages();
    }

    static public bool ET_SaveImagesAndTarget(string mode, float x, float y, float z, float[] slamPoseR = null, float[] slamPoseT = null, int index = -1)
    {
        Debug.LogFormat("ET_SaveImagesAndTarget({0}, {1:F2}, {2:F2}, {3:F2}, ..., ..., {4})", mode, x, y, z, index);
        return ETPlugin_SaveImagesAndTarget(mode, x, y, z, slamPoseR, slamPoseT, index);
    }

    static public bool ET_StartCalibration()
    {
        Debug.Log("ET_StartCalibration()");
        return ETPlugin_StartCalibration();
    }

    static public bool ET_RecordACaliPoint(float x, float y, float z)
    {
        Debug.LogFormat("ET_RecordACaliPoint({0:F2}, {1:F2},{2:F2})", x, y, z);
        return ETPlugin_RecordACaliPoint(x, y, z);
    }

    static public bool ET_FinishCalibration()
    {
        Debug.Log("ET_FinishCalibration()");
        return ETPlugin_FinishCalibration();
    }
    
    // Gaze 结果的 C# 类
    public class EtResult3D
    {
        public Vector3 gazeOrigin = new Vector3();
        public Vector3 gazeDirection = new Vector3();
        public Vector3 gazeOriginL = new Vector3();
        public Vector3 gazeDirectionL = new Vector3();
        public Vector3 gazeOriginR = new Vector3();
        public Vector3 gazeDirectionR = new Vector3();
    }

    // 可重用数据缓冲区
    private static float[] m_trackResultData = new float[30]; 

    static public bool ET_GetTrackResult(out EtResult3D result)
    {
        bool state = false;
        // 假设缓冲区大小为 30 (基于旧版 3D 结果)
        if (m_trackResultData == null)
        {
            m_trackResultData = new float[30];
        }

        bool flag = ETPlugin_GetTrackResult(ref state, m_trackResultData);
        result = new EtResult3D();

        if (flag)
        {
            // 假设数据布局与旧版 GetTrackResult3D 相同
            result.gazeOrigin = new Vector3(m_trackResultData[0], m_trackResultData[1], -m_trackResultData[2]);
            result.gazeDirection = new Vector3(m_trackResultData[3], m_trackResultData[4], -m_trackResultData[5]);
            result.gazeOriginL = new Vector3(m_trackResultData[6], m_trackResultData[7], -m_trackResultData[8]);
            result.gazeDirectionL = new Vector3(m_trackResultData[9], m_trackResultData[10], -m_trackResultData[11]);
            result.gazeOriginR = new Vector3(m_trackResultData[12], m_trackResultData[13], -m_trackResultData[14]);
            result.gazeDirectionR = new Vector3(m_trackResultData[15], m_trackResultData[16], -m_trackResultData[17]);
        }
        
        Debug.Log("ET_GetTrackResult " + "gazeOrigin: " + result.gazeOrigin.ToString("F2") + " gazeDirection: " + result.gazeDirection.ToString("F2"));
        return flag;
    }

    static public bool ET_ETSDKExitAndRelease()
    {
        Debug.Log("ET_ETSDKExitAndRelease()");
        ReleaseImagesBuffer(); // 释放我们分配的非托管内存
        return ETPlugin_ETSDKExitAndRelease();
    }

    static public bool ET_GetExternalMatrixByETCamera(bool isNecessary)
    {
        Debug.LogFormat("ET_GetExternalMatrixByETCamera({0})", isNecessary);
        return ETPlugin_GetExternalMatrixByETCamera(isNecessary);
    }

    static public string ET_GetHardwareSerialNumber(int bufferSize = 256)
    {
        Debug.Log("ET_GetHardwareSerialNumber()");
        StringBuilder serialNumber = new StringBuilder(bufferSize);
        int length = ETPlugin_GetHardwareSerialNumber(serialNumber, bufferSize);
        if (length > 0)
        {
            return serialNumber.ToString();
        }
        return string.Empty;
    }

    static public float ET_GetCamExposure(int eye)
    {
        return ETPlugin_GetCamExposure(eye);
    }

    static public void ET_SetCamExposure(int eye, float exposure)
    {
        ETPlugin_SetCamExposure(eye, exposure);
    }

    static public void ET_ChangeCamExposure(int eye, int flag)
    {
        Debug.LogFormat("ET_ChangeCamExposure(eye: {0}, flag: {1})", eye, flag);
        ETPlugin_ChangeCamExposure(eye, flag);
    }

    static public bool ET_GetAutoAdjustCamExposureState()
    {
        return ETPlugin_GetAutoAdjustCamExposureState();
    }

    static public void ET_CloseAutoAdjustCamExposure()
    {
        Debug.Log("ET_CloseAutoAdjustCamExposure()");
        ETPlugin_CloseAutoAdjustCamExposure();
    }

    static public bool ET_SetMyopiaLens(int eye, int degree)
    {
        Debug.LogFormat("ET_SetMyopiaLens(eye: {0}, degree: {1})", eye, degree);
        return ETPlugin_SetMyopiaLens(eye, degree);
    }

    static public void ET_RECORD_EVENT(string stage, long source_id, long source_id2)
    {
        ETPlugin_RECORD_EVENT(stage, source_id, source_id2);
    }

    static public void ET_ENABLE_TRACING(bool enabled)
    {
        ETPlugin_ENABLE_TRACING(enabled);
    }

    #endregion
}