using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        List<Texture2D> textureEt;
        if (!ETSDK.ET_GetImages(out textureEt))
        {
            Debug.Log("ET_GetImages() failed.");
            return;
        }
        if (!ETSDK.ET_GetTrackResult(out ETSDK.EtResult result))
        {
            Debug.Log("ET_GetTrackResult() failed.");
            return;
        }
        rawImageEt0.texture = textureEt[0];
        rawImageEt1.texture = textureEt[1];

        timer += Time.deltaTime;
        if (timer >= 0.2f)
        {
            timer = 0f;
            LogGazeData(result);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ETSDK.ET_StopStreaming();
            ETSDK.ET_ETSDKExitAndRelease();
        }
    }

    private void LogGazeData(ETSDK.EtResult result)
    {
        Debug.Log("gazeOrigin: " + result.origin.x + ", " + result.origin.y + ", " + result.origin.z);
        Debug.Log("gazeDirection: " + result.direction.x + ", " + result.direction.y + ", " + result.direction.z);
    }
}