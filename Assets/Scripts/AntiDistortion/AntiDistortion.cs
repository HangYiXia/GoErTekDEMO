using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Profiling; // [新增] 必需的命名空间

// 为你的自定义效果提供菜单项
[Serializable, VolumeComponentMenu("Post-processing/Custom/AntiDistortion")]
public sealed class AntiDistortion : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    // --- 1. 声明所有 Shader 参数 ---
    
    [Tooltip("Distance between the two 'eyes'.")]
    public FloatParameter eyeDist = new FloatParameter(0.1f);

    [Tooltip("Field of View Width for distortion calculation.")]
    public FloatParameter fovWidth = new FloatParameter(20f);

    [Tooltip("Field of View Height for distortion calculation.")]
    public FloatParameter fovHeight = new FloatParameter(20f);

    [Tooltip("Screen Width for distortion calculation.")]
    public FloatParameter screenWidth = new FloatParameter(100f);

    [Tooltip("Screen Height for distortion calculation.")]
    public FloatParameter screenHeight = new FloatParameter(100f);
    
    public IntParameter xeryonValue = new IntParameter(0);

    [Tooltip("Red channel distortion coefficients (x=k1, y=k2, z=k3).")]
    public Vector3Parameter kR = new Vector3Parameter(new Vector3(1, 1, 1));

    [Tooltip("Green channel distortion coefficients (x=k1, y=k2, z=k3).")]
    public Vector3Parameter kG = new Vector3Parameter(new Vector3(1, 1, 1));

    [Tooltip("Blue channel distortion coefficients (x=k1, y=k2, z=k3).")]
    public Vector3Parameter kB = new Vector3Parameter(new Vector3(1, 1, 1));

    public BoolParameter enabled = new BoolParameter(true);
    
    // --- [新增] 性能统计相关变量 ---

    // [关键] 使用 static 变量，确保 HDRP 的渲染实例和外部访问的配置实例共享同一个时间值
    private static float s_GpuExecutionTimeMs = 0f;

    // Profiler 标签，必须保证创建 Sampler 和 Get Recorder 时使用的是同一个字符串
    private const string k_ProfilerTag = "AntiDistortion";

    private ProfilingSampler m_ProfilingSampler;
    private Recorder m_GPURecorder;

    // --- 2. 核心方法实现 ---

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public bool IsActive() => m_Material != null && enabled.value;

    private Material m_Material;

    public override void Setup()
    {
        // 找到并创建一个使用你的 Shader 的材质实例
        m_Material = CoreUtils.CreateEngineMaterial("Custom/HDRP_MapShader");
        
        // [新增] 初始化 ProfilingSampler
        m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);

        // [新增] 强制开启编辑器下的 GPU Profiling 区域，防止数据为 0
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU, true);
#endif
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        // 再次检查材质，以防万一
        if (m_Material == null)
        {
            Debug.LogError("m_Material for anti-distortion is null");
            return;
        }

        // [新增] 更新性能数据
        UpdateProfilerData();

        // [修改] 使用成员变量 m_ProfilingSampler，而不是每次 new 一个
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            // --- 3. 将参数从 C# 传递到 Shader ---
            m_Material.SetFloat("_EyeDist", eyeDist.value);
            m_Material.SetFloat("_FOV_Width", fovWidth.value);
            m_Material.SetFloat("_FOV_Height", fovHeight.value);
            m_Material.SetFloat("_Screen_Width", screenWidth.value);
            m_Material.SetFloat("_Screen_Height", screenHeight.value);

            m_Material.SetVector("_K_R", kR.value);
            m_Material.SetVector("_K_G", kG.value);
            m_Material.SetVector("_K_B", kB.value);

            // 执行 Shader
            // 注意：在 HDRP Custom Post Process 中，通常推荐使用 HDUtils.DrawFullScreen
            // 但如果你的 Shader 逻辑依赖 Blit，保留 cmd.Blit 也可以
            cmd.Blit(source, destination, m_Material);
        }
    }

    public override void Cleanup()
    {
        // 销毁我们创建的材质实例
        CoreUtils.Destroy(m_Material);
        
        // [新增] 清理 Recorder
        if (m_GPURecorder != null)
        {
            m_GPURecorder.enabled = false;
            m_GPURecorder = null;
        }
    }

    // --- [新增] 内部数据更新逻辑 ---
    private void UpdateProfilerData()
    {
        // 1. 尝试获取 Recorder
        if (m_GPURecorder == null || !m_GPURecorder.isValid)
        {
            m_GPURecorder = Recorder.Get(k_ProfilerTag);
            m_GPURecorder.enabled = true;
        }

        // 2. 读取数据并存入 static 变量
        if (m_GPURecorder != null && m_GPURecorder.isValid)
        {
            if (m_GPURecorder.gpuElapsedNanoseconds > 0)
            {
                s_GpuExecutionTimeMs = m_GPURecorder.gpuElapsedNanoseconds / 1000000.0f;
            }
            else
            {
                // 如果 GPU 时间不可用（例如部分移动端或 Metal 编辑器模式），回退到 CPU 时间
                s_GpuExecutionTimeMs = m_GPURecorder.elapsedNanoseconds / 1000000.0f;
            }
        }
    }

    // --- [新增] 外部获取接口 ---
    // 其他脚本调用此方法即可获取 GPU 耗时
    public float GetGpuExecTimeMs()
    {
        return s_GpuExecutionTimeMs;
    }
}