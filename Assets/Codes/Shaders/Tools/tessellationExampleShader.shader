Shader "Custom/Tools/Tessellation/Tessellation Example Shader" {
    Properties {
        _MainTex ("Main Tex", 2D) = "white"{}
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _TessFactor ("Tessellation Factor", Range(0.0, 64.0)) = 1.0
    }

    SubShader {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "./Tessellation.hlsl"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        half4 _Color;
        
        half4 frag(d2f i) : SV_Target { 
            half3 finalColor = tex2D(_MainTex, i.uv).rgb * _Color;
        
            return half4(finalColor, 1.0);
        }

        ENDHLSL


        Pass {
            HLSLPROGRAM
            
            #pragma vertex vertTess
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma target 4.6

            ENDHLSL
        }
    }
}