#pragma once

extern "C" {
    #pragma pack(push, 1)
    struct MatDescriptor {
        int rows;
        int cols;
        int type; // OpenCV数据类型，如CV_8UC3
        void* data; // 指向Mat数据的指针
    };
    #pragma pack(pop)

    __declspec(dllexport) bool ETPlugin_Init();  // 初始化算法SDK
    __declspec(dllexport) bool ETPlugin_ReloadCameraStreaming();  // 重新初始化算法SDK
    __declspec(dllexport) bool ETPlugin_StartStreaming();  // 打开相机流，仅打开相机，不开始算法处理
    __declspec(dllexport) bool ETPlugin_StopStreaming();  // 关闭相机流
    __declspec(dllexport) bool ETPlugin_GetETImages(MatDescriptor* outputMats, int* count);  // 获取ET Camera Left，ET Camera Right, Vst Camera图像数据

    __declspec(dllexport) bool ETPlugin_NewDataset();  // 在本地新建数据集
    __declspec(dllexport) bool ETPlugin_NewDatasetWithUserInfo(char* userName);  // 在本地新建数据集
    __declspec(dllexport) bool ETPlugin_SaveImages();  // 保存当前的图像与Target数据到本地
    __declspec(dllexport) bool ETPlugin_SaveImagesAndTarget(char* mode, float x, float y, float z, float* slamPoseR = nullptr, float* slamPoseT = nullptr, int index = -1);  // 保存当前的图像与Target数据到本地

    __declspec(dllexport) bool ETPlugin_StartCalibration();  // 开始校准过程，此接口实现初始化、清空校准数据等功能
    __declspec(dllexport) bool ETPlugin_RecordACaliPoint(float x, float y, float z);  // 记录用于校准的观测数据
    __declspec(dllexport) bool ETPlugin_FinishCalibration();  // 完成校准过程，此接口实现基于观测数据计算眼球参数

    __declspec(dllexport) bool ETPlugin_GetTrackResult(bool& state, float* data); // 输出跟踪数据

    __declspec(dllexport) bool ETPlugin_ETSDKExitAndRelease(); //释放指针退出

    __declspec(dllexport) bool ETPlugin_GetExternalMatrixByETCamera(bool isNecessary); // 无vst情况可用设置true，由ET获得整机到系统的转换矩阵

    __declspec(dllexport) int ETPlugin_GetHardwareSerialNumber(char* serialNumber, int bufferSize); // 获取硬件序列号

    __declspec(dllexport) float ETPlugin_GetCamExposure(int eye); // 获取相机曝光值

    __declspec(dllexport) void ETPlugin_SetCamExposure(int eye, float exposure); // 设置相机曝光值

    __declspec(dllexport) void ETPlugin_ChangeCamExposure(int eye, int flag); // 手动调节camera曝光，flag正负对应每次+-1ms, 0会开启自动曝光

    __declspec(dllexport) bool ETPlugin_GetAutoAdjustCamExposureState(); // 获取自动曝光调节状态

    __declspec(dllexport) void ETPlugin_CloseAutoAdjustCamExposure(); // 关闭自动曝光调节功能，恢复手动曝光

    __declspec(dllexport) bool ETPlugin_SetMyopiaLens(int eye, int degree); // 设置近视镜片度数, 0:左眼，1:右眼，degree:度数 

    __declspec(dllexport) void ETPlugin_RECORD_EVENT(char* stage, long source_id, long source_id2);

    __declspec(dllexport) void ETPlugin_ENABLE_TRACING(bool enabled);
}