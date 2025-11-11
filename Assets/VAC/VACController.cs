using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.IO;


public class VACController : MonoBehaviour
{
    [Header("Decoupled Components")]
    [Tooltip("Hardware Manager 对象 (挂载 XeryonHardwareManager.cs)")]
    public XeryonHardwareManager hardwareManager;
    [Tooltip("Post Process Controller 对象 (挂载 ScenePostProcessController.cs)")]
    public ScenePostProcessController postProcessController;

    [Header("Configuration")]
    public float moveSpeed = 2f;
    public static float focusTime = 0.8f;
    
    private string path;
    private string configTxt = "config.txt";

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
    }

    void OnDestroy()
    {
    }

    private void HandleAgentMoveComplete(float agentDepth)
    {
        if (postProcessController != null)
        {
            postProcessController.UpdateDepthOfField(agentDepth);
        }
        else
        {
            Debug.LogWarning("ScenePostProcessController 未分配！");
        }
        
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
}