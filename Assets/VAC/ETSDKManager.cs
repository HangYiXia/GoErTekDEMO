using UnityEngine;
using UnityEngine.UI; // 1. 包含 UI 命名空间以使用 RawImage

/// <summary>
/// (新) ETSDK 管理器 (已解耦)
/// 职责：
/// 1. 管理 ETSDK 图像的目标 RawImage 引用。
/// 2. 在 Update() 中调用 ETSDK.ET_GetImages()。
/// 3. 将获取到的 Texture2D 应用于 RawImage。
/// </summary>
public class ETSDKManager : MonoBehaviour
{
    [Header("ETSDK Image Targets")]
    public RawImage rawImageEt0;
    public RawImage rawImageEt1;

    private string mode;
    private float x;
    private float y;
    private float z;

    void Start()
    {
        if (!ETSDK.ET_Init())
        {
            Debug.Log("Failed to init.");
        }
    }

    /// <summary>
    /// Update 负责从 ETSDK 拉取图像并更新 RawImages。
    /// </summary>
    void Update()
    {
        // 1. 检查引用是否存在
        if (rawImageEt0 == null || rawImageEt1 == null)
        {
            // 如果不需要显示图像，可以禁用此组件以节省性能
            this.enabled = false;
            return;
        }

        // 2. (逻辑从原 VACController.Update 移来)
        Texture2D textureEt0, textureEt1, textureVst;
        // if (ETSDK.ET_GetImages(out textureEt0, out textureEt1, out textureVst) == false)
        // {
        //     Debug.Log("ET_GetImages() failed.");
        //     return;
        // }
        //if (ETSDK.ET_GetImages(out textureEt0, out textureEt1, out textureVst))
        //{
        //    ETSDK.ET_SaveImagesAndTarget(mode, x, y, z);
        //    Debug.Log("mode: " + mode + " x: " + x + " y: " + y + " z: " + z);
        //}
        if (!ETSDK.ET_GetImages(out textureEt0, out textureEt1, out textureVst))
        {
            Debug.Log("ET_GetImages() failed.");
            return;
        }

        // 3. 应用纹理
        rawImageEt0.texture = textureEt0;
        rawImageEt1.texture = textureEt1;
    }
}