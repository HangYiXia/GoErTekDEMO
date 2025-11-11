using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class ScenePostProcessController : MonoBehaviour
{
    [Header("Post Processing Refs")]
    public PostProcessLayer processLayer;
    public PostProcessVolume processVolume;

    private DepthOfField depth;
    private int curFoveated;

    void Awake()
    {
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
            depth.focusDistance.value = Mathf.Sqrt(Mathf.Pow(agentDepth, 2) + Mathf.Pow(Camera.main.transform.position.y, 2));
            depth.focalLength.value = depth.focusDistance.value * 56.4f + 11f > 70 ? 70 : depth.focusDistance.value * 56.4f + 11f;
        }
    }

    public void SetFoveated(int enable)
    {
        curFoveated = enable;

        if (processLayer != null)
        {
            processLayer.enabled = curFoveated == 0;
        }
    }

    public void SaveStateToPrefs()
    {
        PlayerPrefs.SetInt("curFoveated", curFoveated);
    }

    public void LoadStateFromPrefs()
    {
        curFoveated = PlayerPrefs.GetInt("curFoveated");
        Debug.Log("ScenePostProcessController: Load setting " + " curFoveated | " + curFoveated);
        SetFoveated(curFoveated);
    }
}