using UnityEngine;
using System;
using System.Runtime.InteropServices; // 关键！

public class TestEtAlg : MonoBehaviour
{
    private const string DLL_NAME = "EtAlgInterface";

    // --- 导入DLL函数 ---
    // 我们使用 EntryPoint = "#<序号>" 的方式来精确查找函数
    // 这可以完美绕过 C++ 名称修饰 (name mangling) 的问题

    // 1. 导入 Ordinal 5: bool __cdecl EtAlg::Init(void)
    [DllImport(DLL_NAME, EntryPoint = "#5", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool Init();

    // 2. 导入 Ordinal 8: bool __cdecl EtAlg::StartCalibration(void)
    [DllImport(DLL_NAME, EntryPoint = "#8", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool StartCalibration();

    // 3. 导入 Ordinal 1: bool __cdecl EtAlg::FinishCalibration(void)
    [DllImport(DLL_NAME, EntryPoint = "#1", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool FinishCalibration();

    
    void Start()
    {
        Debug.Log("--- 开始测试 EtAlgInterface.dll ---");

        try
        {
            // --- 步骤 1: 测试 Init() [序号 5] ---
            Debug.Log("正在调用 Init() [Ordinal 5]...");
            bool initResult = Init(); // 调用函数
            Debug.Log($"Init() 调用完毕。返回结果: {initResult}");
            
            // 只有 Init 成功后，才尝试调用其他函数
            if (initResult)
            {
                Debug.Log("Init 成功，继续测试其他函数...");

                // --- 步骤 2: 测试 StartCalibration() [序号 8] ---
                Debug.Log("正在调用 StartCalibration() [Ordinal 8]...");
                bool startResult = StartCalibration();
                Debug.Log($"StartCalibration() 调用完毕。返回结果: {startResult}");

                // --- 步骤 3: 测试 FinishCalibration() [序号 1] ---
                Debug.Log("正在调用 FinishCalibration() [Ordinal 1]...");
                bool finishResult = FinishCalibration();
                Debug.Log($"FinishCalibration() 调用完毕。返回结果: {finishResult}");
            }
            else
            {
                Debug.LogWarning("Init() 返回 false。后续函数将不再测试。");
            }
        }
        catch (DllNotFoundException)
        {
            Debug.LogError($"测试失败：无法找到 '{DLL_NAME}.dll'。");
            Debug.LogError($"请确保 '{DLL_NAME}.dll' 放在 Assets/Plugins 文件夹中。");
        }
        catch (EntryPointNotFoundException ex)
        {
            Debug.LogError($"测试失败：在 '{DLL_NAME}.dll' 中找不到指定的序号（Ordinal）。");
            Debug.LogError($"错误详情: {ex.Message}");
        }
        catch (Exception ex)
        {
            // 捕获其他所有可能的错误（比如DLL内部崩溃）
            Debug.LogError($"调用DLL时发生未知错误: {ex.Message}");
            Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
        }

        Debug.Log("--- EtAlgInterface.dll 测试结束 ---");
    }
}