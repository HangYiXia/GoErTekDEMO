using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System; // Added for Exception handling

/// <summary>
/// (新) GXB 代理文件运行器 (已解耦)
/// 职责：
/// 1. 管理 agent.txt 文件的读取协程 (IEAgentSet)。
/// 2. 持有 AgentCharacter 的引用，并调用其 MoveTo 方法。
/// 3. 提供 AgentSet() 公共方法来切换协程的运行状态。
/// </summary>
public class AgentFileRunner : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("要命令其移动的 AgentCharacter")]
    public AgentCharacter agentCharacter;

    [Header("Settings")]
    [Tooltip("是否在 Start() 时自动启动协程")]
    public bool startOnAwake = true;

    private Coroutine IEAgent = null;

    void Start()
    {
        if (startOnAwake)
        {
            AgentSet();
        }
    }

    /// <summary>
    /// (公共 API) 切换 Agent 协程的运行状态。
    /// 如果当前没有运行 Agent 协程则启动；如果正在运行则停止。
    /// </summary>
    public void AgentSet()
    {
        if (agentCharacter == null)
        {
            Debug.LogError("AgentCharacter 未在 AgentFileRunner 上分配！", this);
            return;
        }

        if (IEAgent == null)
        {
            Debug.LogWarning("AgentFileRunner: 启动协程。", this);
            IEAgent = StartCoroutine(IEAgentSet());
        }
        else
        {
            Debug.LogWarning("AgentFileRunner: 停止协程。", this);
            StopCoroutine(IEAgent);
            IEAgent = null;
        }
    }

    /// <summary>
    /// 协程: 周期性读取 StreamingAssets/agent.txt，解析深度和时间，
    /// 然后调用 AgentCharacter.MoveTo 使代理移动。
    /// </summary>
    private IEnumerator IEAgentSet()
    {
        List<string> _list = new List<string>();
        string[] txt;

        string agentFilePath = Path.Combine(Application.streamingAssetsPath, "agent.txt");
        txt = File.ReadAllLines(agentFilePath, Encoding.UTF8);
        for (int i = 0; i < txt.Length; i++)
        {
            _list.Add(txt[i]);
        }
        for (int index = 0; index < txt.Length; index++)
        {
            string[] temp = _list[index].Split(',');

            float agentDepth = float.Parse(temp[0]);
            float agentTime = float.Parse(temp[1]);
            Debug.Log("agent depth " + agentDepth);

            agentCharacter.MoveTo(Vector3.forward * agentDepth);

            yield return new WaitForSeconds(agentTime);
        }
    }
}