using System;
using UnityEngine;
// using UnityEngine.Rendering.PostProcessing; // (不再需要)
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.IO;
// using System.Collections; // (不再需要)
// using UnityEngine.UI; // (不再需要)

/// <summary>
/// VAC 协调器 (最终版 v5)
/// 职责：
/// 1. 持有所有子系统（Manager/Controller）的引用。
/// 2. 在启动时分发配置 (moveSpeed)。
/// 3. 订阅 AgentCharacter.OnMovementComplete 事件。
/// 4. 在事件回调中协调所有其他系统。
/// 5. (保留) 管理场景重载的调试输入。
/// </summary>
public class VACController : MonoBehaviour
{
    [Header("Decoupled Components")]
    [Tooltip("Hardware Manager 对象 (挂载 XeryonHardwareManager.cs)")]
    public XeryonHardwareManager hardwareManager;
    [Tooltip("Eye Controller 对象 (挂载 AgentEyeController.cs)")]
    public AgentEyeController eyeController;
    [Tooltip("Post Process Controller 对象 (挂载 ScenePostProcessController.cs)")]
    public ScenePostProcessController postProcessController;
    [Tooltip("Agent File Runner 对象 (挂载 AgentFileRunner.cs)")]
    public AgentFileRunner agentFileRunner;
    [Tooltip("(可选) ETSDK Manager 对象 (挂载 ETSDKManager.cs)")]
    public ETSDKManager etsdkManager; // <-- ADDED (尽管本类不再使用它)

    // --- REMOVED: UI 字段 ---
    // public GameObject menuCanvas;
    // public GameObject testCanvas;

    [Header("Configuration")]
    public float moveSpeed = 2f;
    public static float focusTime = 0.8f;
    
    private string path;
    private string configTxt = "config.txt";

    /// <summary>
    /// 只加载本协调器需要的配置 (focusTime, moveSpeed)。
    /// </summary>
    private void ConfigSet()
    {
        Debug.Log("VACController: ConfigSet START");
        path = Application.streamingAssetsPath + "/";
        try
        {
            string configFilePath = Path.Combine(path, configTxt);
            if (!File.Exists(configFilePath))
            {
                Debug.Log("not have path " + configFilePath);
                return;
            }

            List<string> _list = new List<string>();
            string[] txt = File.ReadAllLines(configFilePath, Encoding.UTF8);
            if (txt.Length == 0) return;

            string[] temp = txt[0].Split(',');
            focusTime = float.Parse(temp[2]);
            moveSpeed = float.Parse(temp[3]);

            Debug.Log("focusTime:" + focusTime + " moveSpeed:" + moveSpeed);
        }
        catch (Exception e)
        {
            Debug.Log("error :" + e.ToString());
        }
        Debug.Log("VACController: ConfigSet END");
    }

    void Awake()
    {
        ConfigSet();
    }

    /// <summary>
    /// 1. 订阅 AgentCharacter 事件。
    /// 2. (新) 将 moveSpeed 分发给所有需要的子系统。
    /// </summary>
    void Start()
    {
        if (eyeController != null)
        {
            // 2. 分发配置
            eyeController.moveSpeed = this.moveSpeed;
        }
        else
        {
             Debug.LogError("AgentEyeController 未在 VACController Inspector 中分配！");
        }

    }

    void OnDestroy()
    {
    }

    /// <summary>
    /// 事件回调：协调所有“移动完成后”的逻辑。
    /// </summary>
    private void HandleAgentMoveComplete(float agentDepth)
    {
        // 1. 更新后期处理
        if (postProcessController != null)
        {
            postProcessController.UpdateDepthOfField(agentDepth);
        }
        else
        {
            Debug.LogWarning("ScenePostProcessController 未分配！");
        }
        
        // // 2. 更新眼部动画
        // if (eyeController != null)
        // {
        //     eyeController.UpdateEyeAnim(agentDepth);
        // }

        // 3. 更新硬件
        if (hardwareManager != null)
        {
            int xeryonValue = (int)(600 - 400 * agentDepth);
            hardwareManager.SetXeryonL(xeryonValue);
            hardwareManager.SetXeryonR(xeryonValue);
        }
        else
        {
            Debug.LogWarning("XeryonHardwareManager 未分配！");
        }
    }


    /// <summary>
    /// --- CHANGED ---
    /// Update 现在只包含调试用的场景重载。
    /// UI 和 ETSDK 逻辑均已移除。
    /// </summary>
    void Update()
    {
        // --- REMOVED: UI Key Input ---
        // if (Input.GetKeyUp(KeyCode.M)) { ... }
        // if (Input.GetKeyUp(KeyCode.T)) { ... }

        // (保留调试功能)
        if (Input.GetKeyUp(KeyCode.Space))
        {
            SceneManager.LoadScene("Simple Sample");
        }

        // --- REMOVED: ETSDK Logic ---
        // Texture2D textureEt0, textureEt1, textureVst;
        // if (ETSDK.ET_GetImages(...) == false) { ... }
        // rawImageEt0.texture = textureEt0;
        // rawImageEt1.texture = textureEt1;
    }
    
    // --- REMOVED: RawImage 字段 ---
    // public RawImage rawImageEt0;
    // public RawImage rawImageEt1;

    // --- REMOVED: PlayerPrefs 和 Foveated 方法 ---
    // public void SetFoveated(int enable) { ... }
    // public void SetSave() { ... }
    // private void SetLoad() { ... }
}