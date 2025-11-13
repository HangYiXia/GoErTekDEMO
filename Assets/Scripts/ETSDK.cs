using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Threading.Tasks;

public class ETSDK : MonoBehaviour
{
    private const string DllName = "EyeTrackingPlugin";

    #region MatDescriptor 结构体
    
    // C# 版本的 MatDescriptor 结构体
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MatDescriptor
    {
        public int rows;
        public int cols;
        public int type; // OpenCV 数据类型, e.g., CV_8UC1=0
        public IntPtr data; // 指向Mat数据的指针
    }

    // 对应 TextureFormat.Alpha8
    private const int CV_8UC1 = 0;

    #endregion

    #region P/Invoke DllImport (仅 EyeTrackingPlugin)
    
    [DllImport(DllName)]
    private static extern bool ETPlugin_Init();

    [DllImport(DllName)]
    private static extern bool ETPlugin_ReloadCameraStreaming();

    [DllImport(DllName)]
    private static extern bool ETPlugin_StartStreaming();

    [DllImport(DllName)]
    private static extern bool ETPlugin_StopStreaming();

    // 注意：这里的 outputMats 参数类型是数组，C# 运行时会自动处理固定
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

    #region 图像缓冲区
    
    // 图像属性 (基于原始 SDK)
    private const int ET_WIDTH = 400;
    private const int ET_HEIGHT = 400;
    
    // 托管的 byte[] 缓冲区
    // 我们使用 List 来支持 m_totalImageCount（尽管这里固定为2）
    private static List<byte[]> m_imageEtList = new List<byte[]>();
    // 目标 Texture2D
    private static List<Texture2D> m_textureEt = new List<Texture2D>();

    // MatDescriptor 数组 (用于传递给 C++)
    private static MatDescriptor[] m_matDescriptors = null;
    // 指向非托管内存的指针
    private static List<IntPtr> m_nativeBuffers = new List<IntPtr>();

    private static bool m_matIsInitialized = false;
    private static int m_totalImageCount = 2; // 您指定了只有两个ET相机

    private static void InitImagesBuffer()
    {
        // 防止重复初始化
        if (m_matIsInitialized)
        {
            Debug.Log("InitImagesBuffer 已经初始化过了。");
            return;
        }

        Debug.Log("InitImagesBuffer 开始初始化...");

        // 1. 创建 MatDescriptor 数组
        m_matDescriptors = new MatDescriptor[m_totalImageCount];
        
        int bufferSize = ET_WIDTH * ET_HEIGHT; // Alpha8 (8UC1) 是 1-byte per pixel

        for (int i = 0; i < m_totalImageCount; i++)
        {
            // 2. 分配托管 byte 数组
            m_imageEtList.Add(new byte[bufferSize]);

            // 3. 创建 Texture2D
            // 使用 Alpha8, 它只使用一个通道，非常适合灰度图，且高效
            m_textureEt.Add(new Texture2D(ET_WIDTH, ET_HEIGHT, TextureFormat.Alpha8, false));

            // 4. 分配非托管内存
            IntPtr nativeBuffer = Marshal.AllocHGlobal(bufferSize);
            m_nativeBuffers.Add(nativeBuffer);

            // 5. 设置 MatDescriptor 指向非托管内存
            m_matDescriptors[i] = new MatDescriptor
            {
                rows = ET_HEIGHT,
                cols = ET_WIDTH,
                type = CV_8UC1, // 8-bit single channel
                data = nativeBuffer
            };
        }

        m_matIsInitialized = true;
        Debug.Log("InitImagesBuffer 初始化完成。");
    }

    private static void ReleaseImagesBuffer()
    {
        if (m_matIsInitialized)
        {
            Debug.Log("ReleaseImagesBuffer 释放内存...");
            // 释放非托管内存
            foreach (var bufferPtr in m_nativeBuffers)
            {
                Marshal.FreeHGlobal(bufferPtr);
            }

            // 重置所有引用
            m_nativeBuffers.Clear();
            m_imageEtList.Clear();
            m_textureEt.Clear();
            m_matDescriptors = null;
            
            m_matIsInitialized = false;
        }
    }

    #endregion

    #region 公共静态 SDK 方法

    static public bool ET_Init()
    {
        Debug.Log("ET_Init()");
        InitImagesBuffer(); // 在 Init 时就准备好缓冲区
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
        InitImagesBuffer(); // 确保已初始化
        return ETPlugin_StartStreaming();
    }

    static public bool ET_StopStreaming()
    {
        Debug.Log("StopStreaming()");
        return ETPlugin_StopStreaming();
    }

    static public bool ET_GetImages(out List<Texture2D> textureEtList)
    {
        //Debug.Log("ET_GetImages()");
        
        // 确保已初始化
        if (!m_matIsInitialized)
        {
            Debug.LogError("ET_GetImages: 缓冲区未初始化!");
            textureEtList = m_textureEt; // 返回空的或旧的 list
            return false;
        }
        
        int count = m_totalImageCount;
        bool flag = ETPlugin_GetETImages(m_matDescriptors, ref count);
        
        if (flag && count > 0)
        {
            try
            {
                for(int i = 0; i < m_totalImageCount; i++)
                {
                    // 1. 从非托管内存 (m_nativeBuffers[i]) 复制到 托管数组 (m_imageEtList[i])
                    Marshal.Copy(m_nativeBuffers[i], m_imageEtList[i], 0, m_imageEtList[i].Length);
                    
                    // 2. 加载到 Texture2D
                    m_textureEt[i].LoadRawTextureData(m_imageEtList[i]);
                    m_textureEt[i].Apply();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ET_GetImages: 拷贝图像数据时出错. {ex.Message}");
                flag = false;
            }
        }
        
        textureEtList = m_textureEt;
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

    static public bool ET_SaveImagesAndTarget(string mode, float x, float y, float z, float[] slamPoseR, float[] slamPoseT, int index = -1)
    {
        Debug.LogFormat("ET_SaveImagesAndTarget({0}, {1:F2}, {2:F2}, {3:F2}, ..., ..., {4})", mode, x, y, z, index);
        // 如果 slamPoseR 或 T 为 null，传入一个空数组或默认数组可能更安全
        if (slamPoseR == null) slamPoseR = new float[3];
        if (slamPoseT == null) slamPoseT = new float[3];
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

    // 使用原始 SDK 中的异步方法，防止阻塞主线程
    static public bool ET_FinishCalibration()
    {
        Debug.Log("ET_FinishCalibration() (异步启动)");
        StartFinishCalibrationTask();
        return true;
    }

    async static void StartFinishCalibrationTask()
    {
        bool result = await Task.Run(() => {
            return ETPlugin_FinishCalibration();
        });
        Debug.Log("FinishCalibration() 异步任务完成，结果: " + result);
    }
    
    // Gaze 结果的 C# 类 (基于原始 SDK 结构)
    public class EtResult
    {
        public bool eyeSucc;      // 追踪是否成功
        public Vector3 origin;    // 3D 组合注视点起点
        public Vector3 direction; // 3D 组合注视点方向
        // public Vector3 originLeft; // (数据存在于 buffer[6-8] 中)
        // public Vector3 directionLeft; // (数据存在于 buffer[9-11] 中)
        // public Vector3 originRight; // (数据存在于 buffer[12-14] 中)
        // public Vector3 directionRight; // (数据存在于 buffer[15-17] 中)
        public float depth;       // 深度
        public long sourceId;
        public long sourceId2;
    }

    // 可重用数据缓冲区
    private static float[] m_trackResultData = new float[21]; // 基于原始 SDK，大小为 21

    static public bool ET_GetTrackResult(out EtResult result)
    {
        bool state = false;
        bool flag = ETPlugin_GetTrackResult(ref state, m_trackResultData);
        
        result = new EtResult();
        result.eyeSucc = state;

        if (flag && state)
        {
            // 注意：Unity 使用左手坐标系 (Z轴向前为正)
            // 原始 SDK (ETSDK.cs) 没有反转Z轴，但您的新版 (ETSDK_new.cs) 反转了Z轴。
            // 我将保留您新版中的反转，这通常是为了适配 Unity 坐标系。
            result.origin = new Vector3(m_trackResultData[0], m_trackResultData[1], -m_trackResultData[2]);
            result.direction = new Vector3(m_trackResultData[3], m_trackResultData[4], -m_trackResultData[5]);
            
            // 如果需要，您可以取消注释以下内容来获取左/右眼数据
            // result.originLeft = new Vector3(m_trackResultData[6], m_trackResultData[7], -m_trackResultData[8]);
            // result.directionLeft = new Vector3(m_trackResultData[9], m_trackResultData[10], -m_trackResultData[11]);
            // result.originRight = new Vector3(m_trackResultData[12], m_trackResultData[13], -m_trackResultData[14]);
            // result.directionRight = new Vector3(m_trackResultData[15], m_trackResultData[16], -m_trackResultData[17]);

            result.depth = m_trackResultData[18];
            result.sourceId = (long) m_trackResultData[19];
            result.sourceId2 = (long) m_trackResultData[20];
        }
        
        // Debug.Log("ET_GetTrackResult " + "gazeOrigin: " + result.origin.ToString("F2") + " gazeDirection: " + result.direction.ToString("F2"));
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

    // 使用您新版中的 StringBuilder，这很好
    static public string ET_GetHardwareSerialNumber(int bufferSize = 256)
    {
        Debug.Log("ET_GetHardwareSerialNumber()");
        StringBuilder serialNumber = new StringBuilder(bufferSize);
        int length = ETPlugin_GetHardwareSerialNumber(serialNumber, bufferSize);
        if (length > 0)
        {
            return serialNumber.ToString();
        }
        Debug.LogError("获取硬件序列号失败，返回空字符串。");
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

    // --- 从原始 SDK 中恢复的辅助函数 ---

    static public void IncreaseCamExposure(int eye)
    {
        ETPlugin_ChangeCamExposure(eye, 10);
    }

    static public void DecreaseCamExposure(int eye)
    {
        ETPlugin_ChangeCamExposure(eye, -10);
    }

    static public void ResetCamExposure()
    {
        ETPlugin_ChangeCamExposure(0,0);
    }

    static public bool ET_GetAutoAdjustCamExposureState()
    {
        return ETPlugin_GetAutoAdjustCamExposureState();
    }

    static public void ChangeAutoAdjustCamExposureState(bool enabled)
    {
        if (enabled == false)
        {
            // 关闭自动曝光，恢复手动
            ETPlugin_CloseAutoAdjustCamExposure();
        }
        else
        {
            // 恢复（重置）为自动曝光
            ResetCamExposure();
        }
    }
    // --- 恢复结束 ---

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