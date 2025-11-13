Shader "Hidden/Shader/GaussianBlurSinglePass" // 重命名
{
    Properties
    {
        _MainTex ("Main Texture", 2DArray) = "white" {}
        _Radius ("Blur Radius", Range(0, 60)) = 3
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 d3d12 vulkan metal
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURE2D_X(_MainTex);
    float4 _MainTex_TexelSize;
    float _Radius;
    
    float _NearStart;
    float _NearEnd;
    float _FarStart;
    float _FarEnd;
    
    // ... Vert 函数 和 CacBlurFactor_V2 函数保持不变 ...
    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    float CacBlurFactor_V2(float linearEyeDepth)
    {
        float nearFactor = smoothstep(_NearEnd, _NearStart, linearEyeDepth);
        float farFactor = smoothstep(_FarStart, _FarEnd, linearEyeDepth);
        return max(nearFactor, farFactor);
    }
    // ...


    float4 GaussianBlurH(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;

 
        // 1. Sigma 现在直接受 Radius 影响，我们去掉了 min(..., 3) 的限制
        float sigma = max(_Radius * 0.25, 0.5);
        // 2. 半径可以设置得更大，比如 15 (总宽度 31)
        int halfWidth = min((int)(sigma * 3.0), 15); // 允许更大的核
        
        float3 blurColor = 0.0;
        float sum = 0.0;
        
        // 获取 _MainTex (src 或 tempRT) 的正确尺寸
        float2 mainTexSize = _MainTex_TexelSize.zw;
        // 只在一个方向上循环
        for (int i = -halfWidth; i <= halfWidth; ++i)
        {
            float2 offset = float2(1, 0) * i * _MainTex_TexelSize.xy;
            float w = exp(-(i*i) / (2.0 * sigma * sigma));
            
            blurColor += LOAD_TEXTURE2D_X(_MainTex, uint2((uv + offset) * mainTexSize)).rgb * w;
            sum += w;
        }
        blurColor /= sum;
        
        return float4(blurColor, 1.0);
    }

     float4 GaussianBlurV(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;

 
        // 1. Sigma 现在直接受 Radius 影响，我们去掉了 min(..., 3) 的限制
        float sigma = max(_Radius * 0.25, 0.5);
        // 2. 半径可以设置得更大，比如 15 (总宽度 31)
        int halfWidth = min((int)(sigma * 3.0), 15); // 允许更大的核
        
        float3 blurColor = 0.0;
        float sum = 0.0;
        
        // 获取 _MainTex (src 或 tempRT) 的正确尺寸
        float2 mainTexSize = _MainTex_TexelSize.zw;
        // 只在一个方向上循环
        for (int i = -halfWidth; i <= halfWidth; ++i)
        {
            float2 offset = float2(0, 1) * i * _MainTex_TexelSize.xy;
            float w = exp(-(i*i) / (2.0 * sigma * sigma));
            
            blurColor += LOAD_TEXTURE2D_X(_MainTex, uint2((uv + offset) * mainTexSize)).rgb * w;
            sum += w;
        }
        blurColor /= sum;
        
        return float4(blurColor, 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        
        // Pass 0: Horizontal Blur
        Pass
        {
            Name "GaussianBlurHorizontal"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlurH
            ENDHLSL
        }
        
        // Pass 1: Vertical Blur + Depth Blend
        Pass
        {
            Name "GaussianBlurVertical"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlurV
            ENDHLSL
        }
    }
    Fallback Off
}