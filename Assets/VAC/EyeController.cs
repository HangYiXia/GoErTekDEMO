using UnityEngine;
using DG.Tweening;

public class AgentEyeController : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 2f; // 此动画也需要 moveSpeed

    /// <summary>
    /// (可选) 允许协调器在运行时更新此组件的移动速度。
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}