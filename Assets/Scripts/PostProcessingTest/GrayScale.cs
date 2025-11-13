using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/GrayScale")]
public sealed class GrayScale : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderName = "Hidden/Shader/GrayScale";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
        {
            m_Material = new Material(Shader.Find(kShaderName));
        }
        else
        {
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume GrayScale is unable to load.");
        }
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetTexture("_MainTex", source);

        // 方法1：使用正确的DrawFullScreen参数（推荐）
        HDUtils.DrawFullScreen(cmd, m_Material, destination);

        // 或者方法2：如果需要更多控制，可以使用这个版本
        // HDUtils.DrawFullScreen(cmd, m_Material, destination, 
        //     shaderPassId: 0, 
        //     properties: null);
    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);
}