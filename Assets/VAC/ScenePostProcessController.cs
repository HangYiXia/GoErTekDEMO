using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// (新) 场景后期处理控制器 (已解耦)
/// 职责：
/// 1. 管理 PostProcessLayer 和 PostProcessVolume 的引用。
/// 2. 封装 SetFoveated(int enable) 逻辑，切换 Layer 状态。
/// 3. 封装 UpdateDepthOfField(float depth) 逻辑，更新焦距。
/// 4. 独立管理 curFoveated 状态的加载与保存 (PlayerPrefs)。
/// </summary>
public class ScenePostProcessController : MonoBehaviour
{
    [Header("Post Processing Refs")]
    public PostProcessLayer processLayer;
    public PostProcessVolume processVolume;

    private DepthOfField depth;
    private int curFoveated;

    void Awake()
    {
        // 在启动时加载并应用 foveated 设置
        LoadStateFromPrefs();
    }

    /// <summary>
    /// (公共 API) 更新景深（Depth of Field）效果。
    /// 根据 agentDepth 计算并设置焦距和焦距长度。
    /// </summary>
    /// <param name="agentDepth">代理的 Z 轴深度</param>
    public void UpdateDepthOfField(float agentDepth)
    {
        if (processVolume == null)
        {
            Debug.LogWarning("processVolume 未分配！", this);
            return;
        }

        // 尝试获取景深设置
        if (processVolume.profile.TryGetSettings<DepthOfField>(out depth))
        {
            // (逻辑从原 VACController.HandleAgentMoveComplete 移来)
            depth.focusDistance.value = Mathf.Sqrt(Mathf.Pow(agentDepth, 2) + Mathf.Pow(Camera.main.transform.position.y, 2));
            depth.focalLength.value = depth.focusDistance.value * 56.4f + 11f > 70 ? 70 : depth.focusDistance.value * 56.4f + 11f;
        }
    }

    /// <summary>
    /// (公共 API) 启用/禁用 foveated 渲染。
    /// 当 enable=0 时，启用 PostProcessLayer。
    /// </summary>
    public void SetFoveated(int enable)
    {
        curFoveated = enable;

        if (processLayer != null)
        {
            processLayer.enabled = curFoveated == 0;
        }
    }

    /// <summary>
    /// (公共 API) 将 curFoveated 状态保存到 PlayerPrefs。
    /// </summary>
    public void SaveStateToPrefs()
    {
        PlayerPrefs.SetInt("curFoveated", curFoveated);
    }

    /// <summary>
    /// (公共 API) 从 PlayerPrefs 加载 curFoveated 状态并应用。
    /// </summary>
    public void LoadStateFromPrefs()
    {
        curFoveated = PlayerPrefs.GetInt("curFoveated");
        Debug.Log("ScenePostProcessController: Load setting " + " curFoveated | " + curFoveated);
        SetFoveated(curFoveated);
    }
}