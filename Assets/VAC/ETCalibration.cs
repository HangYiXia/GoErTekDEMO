using UnityEngine;
using System.Collections;

public class ETCalibrationManager : MonoBehaviour
{
    [Header("校准配置")]
    [Tooltip("用于向用户显示的视觉目标 (例如一个 Image, Sprite 或 3D Sphere)")]
    public GameObject calibrationTarget;

    [Tooltip("每个校准点显示多长时间 (秒)")]
    public float timePerPoint = 2.0f;

    [Tooltip("两个点之间的等待时间 (秒)")]
    public float timeBetweenPoints = 0.5f;

    [Header("校准点 (世界坐标)")]
    [Tooltip("您希望用户注视的3D世界坐标点列表")]
    public Vector3[] calibrationPoints = new Vector3[]
    {
        new Vector3(0, 0, 2),
        new Vector3(-1, 0.5f, 2),
        new Vector3(1, 0.5f, 2),
        new Vector3(-1, -0.5f, 2),
        new Vector3(1, -0.5f, 2)
    };

    private bool isCalibrating = false;

    void Start()
    {
        // 确保校准目标在开始时是隐藏的
        if (calibrationTarget != null)
        {
            calibrationTarget.SetActive(false);
        }
        else
        {
            Debug.LogError("校准目标 (Calibration Target) 未设置！");
        }
    }

    void Update()
    {
        // 按 'C' 键开始校准
        if (Input.GetKeyDown(KeyCode.C) && !isCalibrating)
        {
            if (calibrationPoints == null || calibrationPoints.Length == 0)
            {
                Debug.LogError("没有定义校准点！");
                return;
            }
            StartCoroutine(RunCalibrationProcess());
        }
    }

    private IEnumerator RunCalibrationProcess()
    {
        isCalibrating = true;
        Debug.Log("--- 校准流程开始 ---");

        if (!ETSDK.ET_StartCalibration())
        {
            Debug.LogError("ETSDK.ET_StartCalibration() 失败！正在中止。");
            isCalibrating = false;
            yield break;
        }

        for (int i = 0; i < calibrationPoints.Length; i++)
        {
            Vector3 point = calibrationPoints[i];
            Debug.LogFormat("显示校准点 {0}/{1} 于 {2}", i + 1, calibrationPoints.Length, point);

            if (calibrationTarget != null)
            {
                calibrationTarget.transform.position = point;
                calibrationTarget.SetActive(true);
            }

            // 等待 'timePerPoint' 秒，让用户注视
            yield return new WaitForSeconds(timePerPoint);

            Debug.Log("...记录观测数据...");
            ETSDK.ET_RecordACaliPoint(point.x, point.y, point.z);

            if (calibrationTarget != null)
            {
                calibrationTarget.SetActive(false);
            }
            yield return new WaitForSeconds(timeBetweenPoints);
        }

        Debug.Log("...所有点均已记录。正在计算校准模型...");
        if (ETSDK.ET_FinishCalibration())
        {
            Debug.Log("--- 校准成功！ ---");
        }
        else
        {
            Debug.LogError("--- 校准失败！(ETSDK.ET_FinishCalibration() 返回 false) ---");
        }

        isCalibrating = false;
    }
}