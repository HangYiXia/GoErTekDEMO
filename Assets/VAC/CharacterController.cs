using UnityEngine;
using DG.Tweening;
using UnityEngine.Events; // 1. 导入 UnityEvent

/// <summary>
/// (新) GXB 代理角色控制器 (已解耦)
/// 职责：
/// 1. 持有 GXB 模型的动画、网格和材质引用。
/// 2. 封装使用 DOTween 的 MoveTo 移动逻辑。
/// 3. 在移动完成时，只处理自己的动画/材质状态。
/// 4. (关键) 广播一个 OnMovementComplete 事件，通知其他系统移动已完成，并传递深度值。
/// </summary>


public class AgentCharacter : MonoBehaviour
{
    [Header("Agent Components")]
    public Animation gxbAnim;
    public SkinnedMeshRenderer gxbMesh;
    public Material gxbMat_hi, gxbMat_run;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    [Header("Events")]
    // 3. 当移动完成时，将触发此事件
    public AgentMoveCompleteEvent OnMovementComplete;

    private Tweener moveTweener;

    /// <summary>
    /// --- CHANGED ---
    /// 通过 DOTween 控制代理移动到给定 pos 的 z 深度位置。
    /// OnComplete 回调现在只处理角色的内部状态（动画, 材质），
    /// 并广播 OnMovementComplete 事件。
    /// </summary>
    public void MoveTo(Vector3 pos)
    {
        // 1. 开始移动：播放跑步动画，切换材质
        gxbAnim.Play("run");
        gxbMesh.material = gxbMat_run;
        gxbAnim.transform.DODynamicLookAt(new Vector3(pos.x, 0, pos.z), moveSpeed / 2);
        
        // 2. 杀死旧的 Tweener 并开始新的移动
        if (moveTweener != null) moveTweener.Kill();
        
        moveTweener = gxbAnim.transform.DOMove(new Vector3(0f, 0, pos.z), moveSpeed).OnComplete(() =>
        {
            // 3. 到达目的地：处理自己的状态
            gxbAnim.Play("hi");
            gxbMesh.material = gxbMat_hi;
            gxbAnim.transform.DODynamicLookAt(new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z), moveSpeed / 2);

            // 4. (关键) 通知所有监听者：我移动完成了，这是我的深度
            if (OnMovementComplete != null)
            {
                OnMovementComplete.Invoke(pos.z);
            }

            // --- 所有原有的外部逻辑均已移除 ---
            // processVolume.profile.TryGetSettings<DepthOfField>(out depth); // <-- REMOVED
            // depth.focusDistance.value = ...; // <-- REMOVED
            // UpdateEyeAnim(pos.z); // <-- REMOVED
            // hardwareManager.SetXeryonL(...); // <-- REMOVED
            // hardwareManager.SetXeryonR(...); // <-- REMOVED
        });
    }

    /// <summary>
    /// (可选) 如果在运行时 moveSpeed 发生变化，可以在这里更新
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}

// 2. 定义一个可以传递 float (深度) 的 UnityEvent
[System.Serializable]
public class AgentMoveCompleteEvent : UnityEvent<float> { }
