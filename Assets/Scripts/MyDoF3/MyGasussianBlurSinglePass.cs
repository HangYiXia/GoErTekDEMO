using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Profiling; // [新增] 必须引入此命名空间

[System.Serializable, VolumeComponentMenu("Post-processing/Custom/My Gaussian Blur (Single Pass)")]
public sealed class MyGaussianBlurSinglePass : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("模糊半径（像素），值越大，模糊程度越高。")]
    public ClampedFloatParameter radius = new ClampedFloatParameter(10f, 0f, 60f);
    
    [Tooltip("对焦距离")]
    public MinFloatParameter focalLength = new MinFloatParameter(10f, 0f);
    
    [Tooltip("镜头焦距")]
    public MinFloatParameter f = new MinFloatParameter(0.017f, 0f);
    
    [Tooltip("光圈直径")]
    public MinFloatParameter A = new MinFloatParameter(0.002f, 0f);
    
    [Tooltip("最大弥散圆直径")]
    public MinFloatParameter maxCoCsize = new MinFloatParameter(120f, 0f);
    
    public BoolParameter enabled = new BoolParameter(true);


    public bool IsActive() => m_Material != null &&
                              radius.value > 0;

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    private Material m_Material;

    // [新增] 用于在 Inspector 或其他脚本中查看 GPU 耗时（单位：毫秒）
    private static float s_gpuExecutionTimeMs = 0f;

    // [新增] 缓存 ProfilingSampler，保证 Name ID 一致
    private ProfilingSampler m_ProfilingSampler;
    // [新增] 用于读取数据的 Recorder
    private Recorder m_GPURecorder;
    
    // 定义 Profiler 标签名称，确保 Recorder 能找到它
    private const string k_ProfilerTag = "My Gaussian Blur (Single Pass)";
    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/Shader/GaussianBlurSinglePass");
        // [新增] 初始化 Sampler
        m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);
        
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU, true);
#endif
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle src, RTHandle dest)
    {
        // [新增] 每一帧尝试获取 Recorder 数据
        UpdateProfilerData();

        // [修改] 使用缓存的 m_ProfilingSampler
        using (new ProfilingScope(cmd, m_ProfilingSampler)) 
        {
            if (m_Material == null)
            {
                HDUtils.BlitCameraTexture(cmd, src, dest);
                return;
            }

            m_Material.SetFloat("_Radius", radius.value);
            m_Material.SetFloat("_F", focalLength.value);
            m_Material.SetFloat("_A", A.value);
            m_Material.SetFloat("_f", f.value);
            m_Material.SetFloat("_MaxCocSize", maxCoCsize.value);
            
            m_Material.SetTexture("_MainTex", src);
            
            HDUtils.DrawFullScreen(cmd, m_Material, dest);
        }
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
        // [新增] 禁用 Recorder
        if (m_GPURecorder != null)
        {
            m_GPURecorder.enabled = false;
            m_GPURecorder = null;
        }
    }
    
    // [新增] 读取 GPU 时间的逻辑
    private void UpdateProfilerData()
    {
        // 懒加载 Recorder，因为在 Setup 时系统可能还没注册这个 Tag
        if (m_GPURecorder == null || !m_GPURecorder.isValid)
        {
            m_GPURecorder = Recorder.Get(k_ProfilerTag);
            m_GPURecorder.enabled = true;
        }

        // 读取上一帧的数据
        if (m_GPURecorder != null && m_GPURecorder.isValid)
        {
            // gpuElapsedNanoseconds 返回纳秒，除以 1,000,000 得到毫秒
            if (m_GPURecorder.gpuElapsedNanoseconds > 0)
            {
                s_gpuExecutionTimeMs = m_GPURecorder.gpuElapsedNanoseconds / 1000000.0f;
            }
            // 如果不支持 GPU Profiling，尝试读取 CPU 时间作为参考（虽然对 Shader 意义不大）
            else 
            {
                s_gpuExecutionTimeMs = m_GPURecorder.elapsedNanoseconds / 1000000.0f; 
            }
        }
    }

    public float GetGpuExecTimeMs()
    {
        return s_gpuExecutionTimeMs;
    }
}