using UnityEngine;
using UnityEngine.UI; // 1. 包含 UI 命名空间以使用 RawImage
using System;
using System.Runtime.InteropServices;

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
    private float timer = 0f;

    void Start()
    {
        if (!ETSDK.ET_Init())
        {
            Debug.Log("Failed to init.");
        }
        ETSDK.ET_StartStreaming();
    }


    void Update()
    {
        Texture2D textureEt0, textureEt1, textureVst;
        if (!ETSDK.ET_GetImages(out textureEt0, out textureEt1, out textureVst))
        {
            Debug.Log("ET_GetImages() failed.");
            return;
        }
        rawImageEt0.texture = textureEt0;
        rawImageEt1.texture = textureEt1;

        timer += Time.deltaTime;
        if (timer >= 2.0f)
        {
            timer = 0f;
            LogGazeData();
        }

    }
    private void LogGazeData()
    {
        if (ETSDK.ET_GetTrackResult(out ETSDK.EtResult3D result))
        {
            Debug.Log("gazeOrigin: " + result.gazeOrigin.x + ", " + result.gazeOrigin.y + ", " + result.gazeOrigin.z);
            Debug.Log("gazeDirection: " + result.gazeDirection.x + ", " + result.gazeDirection.y + ", " + result.gazeDirection.z);
        }
    }
}