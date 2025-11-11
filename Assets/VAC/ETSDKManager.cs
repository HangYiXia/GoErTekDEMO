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

    public Transform focusPoint;
    public Transform eyeGaze;
    public VACController vacController;
    public float test = 1f;

    #region 
    private float eyeGazeX;
    private float eyeGazeY;
    private Vector3 origin, direction;
    public float Max;
    public float Min;
    #endregion         
    public Transform testpoint;


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


        ETSDK.EtResult3D result = new ETSDK.EtResult3D();
        if (ETSDK.ET_GetTrackResult(out result))
        {
            //eyeGaze.localPosition = result.gazeOrigin;

            origin = result.gazeOrigin / 1000;
            direction = result.gazeDirection / 1000;


            testpoint.localPosition = origin + direction * (test - origin.z) / direction.z;

            eyeGazeX = Mathf.Atan(testpoint.localPosition.x / testpoint.localPosition.z) * 180 / Mathf.PI;
            eyeGazeY = -Mathf.Atan(testpoint.localPosition.y / testpoint.localPosition.z) * 180 / Mathf.PI;

            eyeGaze.transform.LookAt(testpoint);
            eyeGaze.localPosition = origin;
        }
        else
        {
            eyeGazeX += Input.GetAxis("Mouse X");
            eyeGazeY -= Input.GetAxis("Mouse Y");

            eyeGazeX = Mathf.Clamp(eyeGazeX, Min, Max);
            eyeGazeY = Mathf.Clamp(eyeGazeY, Min, Max);

            eyeGaze.localEulerAngles = new Vector3(eyeGazeY, eyeGazeX, 0);
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(eyeGaze.position, eyeGaze.forward, out hitInfo))
        {
            //Debug.Log(hitInfo.point);
            //Debug.Log(hitInfo.collider.name);
            FocusTarget focusTarget = hitInfo.collider.GetComponent<FocusTarget>();
            if (focusTarget)
            {
                focusTarget.FoucsOn();
            }
        }
        Debug.DrawRay(eyeGaze.position, eyeGaze.position + eyeGaze.forward * 10, Color.red);

        focusPoint.position = hitInfo.point;
    }
}