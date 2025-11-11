Shader "Custom/Gexiaobao/GexiaobaoToonShader"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.1, 0.0, 1.0)


    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            

            sampler2D _MainTex;
            float4 _MainTex_ST;

            half4 _Color;
            half4 _ShadowColor;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert (a2v v) {
                v2f o;

                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = TransformObjectToWorld(v.vertex);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);

                return o;
            }

            half4 frag (v2f i) : SV_Target {
                float3 worldNormal = normalize(i.worldNormal);
                float3 worldLightDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                
                half3 albedo = tex2D(_MainTex, i.uv).rgb;

                // DirectionalLightData mainLight = GetMainLight();
                // float3 lightDir = -mainLight.forward;   // 光的朝向（从光源指向物体）
                // float3 lightColor = mainLight.color.rgb * mainLight.intensity;

                half3 diffuse = _ShadowColor.rgb * dot(worldNormal, worldLightDir);

                half3 color = _Color.rgb * (albedo);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}
