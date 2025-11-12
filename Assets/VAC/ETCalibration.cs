using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 管理 ETSDK 的校准流程。
/// 
/// 职责：
/// 1. 提供一个可配置的校准点列表 (在3D世界空间中)。
/// 2. 提供一个UI对象作为视觉目标。
/// 3. 按下 'C' 键时，启动一个协程 (Coroutine) 来执行校准。
/// 4. 依次显示每个点，等待用户注视，然后调用 ETSDK.ET_RecordACaliPoint()。
/// 5. 在所有点都记录完毕后，调用 ETSDK.ET_FinishCalibration()。
/// </summary>
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
        // 这是一个5点示例 (假设在Z=2米处)
        // 您应该根据您的场景重新配置这些点
        new Vector3(0, 0, 2),    // 中心
        new Vector3(-1, 0.5f, 2), // 左上
        new Vector3(1, 0.5f, 2),  // 右上
        new Vector3(-1, -0.5f, 2),// 左下
        new Vector3(1, -0.5f, 2)  // 右下
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
        // 按 'C' 键开始校准 (模仿 C++ 示例)
        if (Input.GetKeyDown(KeyCode.C) && !isCalibrating)
        {
            // 确保我们有校准点
            if (calibrationPoints == null || calibrationPoints.Length == 0)
            {
                Debug.LogError("没有定义校准点！");
                return;
            }
            // 启动校准流程
            StartCoroutine(RunCalibrationProcess());
        }
    }

    /// <summary>
    /// 执行校准流程的协程
    /// </summary>
    private IEnumerator RunCalibrationProcess()
    {
        isCalibrating = true;
        Debug.Log("--- 校准流程开始 ---");

        // 1. 步骤一：开始校准
        //    通知SDK清空旧数据并准备接收新数据
        if (!ETSDK.ET_StartCalibration())
        {
            Debug.LogError("ETSDK.ET_StartCalibration() 失败！正在中止。");
            isCalibrating = false;
            yield break; // 退出协程
        }

        // 2. 步骤二：循环记录所有校准点
        for (int i = 0; i < calibrationPoints.Length; i++)
        {
            Vector3 point = calibrationPoints[i];
            Debug.LogFormat("显示校准点 {0}/{1} 于 {2}", i + 1, calibrationPoints.Length, point);

            // 显示目标
            if (calibrationTarget != null)
            {
                // (注意: 您可能需要一个辅助函数将3D世界坐标转换为屏幕坐标
                //  来放置UI元素，但如果 'calibrationTarget' 是一个3D对象，
                //  直接设置其世界坐标即可)
                calibrationTarget.transform.position = point;
                calibrationTarget.SetActive(true);
            }

            // 等待 'timePerPoint' 秒，让用户注视
            yield return new WaitForSeconds(timePerPoint);

            // 记录此点的观测数据
            Debug.Log("...记录观测数据...");
            ETSDK.ET_RecordACaliPoint(point.x, point.y, point.z);

            // 隐藏目标并稍作停顿
            if (calibrationTarget != null)
            {
                calibrationTarget.SetActive(false);
            }
            yield return new WaitForSeconds(timeBetweenPoints);
        }

        // 3. 步骤三：完成校准
        Debug.Log("...所有点均已记录。正在计算校准模型...");
        // 通知SDK基于所有观测数据计算眼球参数
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