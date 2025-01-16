Shader "Custom/ErdTreeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 5)) = 1
        _AlphaClip ("Alpha Clip", Range(-1, 1)) = 0
        _RimPower ("Rim Power", Range(0, 10)) = 2.5
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _AlphaMax ("Alpha Max", Range(0, 5)) = 1
        _Brightness ("Brightness", Range(0, 5)) = 1
        [Header(Tint)]
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _TintIntensity ("Tint Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 100
        
        Pass
        {
            Tags{ "LightMode" = "UniversalGBuffer" }
            ZWrite On
            ColorMask 0
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _Color;
        float _Alpha;
        float _AlphaClip;
        half _RimPower;
        float4 _RimColor;
        half _Brightness;
        float _TintIntensity;
        CBUFFER_END
        ENDHLSL
        
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" "Queue" = "Transparent"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float fogFactor: TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);    // <-
                o.posWS = TransformObjectToWorld(v.vertex);     // <-
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
       ;        VertexNormalInputs normalInput = GetVertexNormalInputs(v.normal, v.tangent);
                o.normal = normalInput.normalWS;
                
                o.fogFactor = ComputeFogFactor(o.vertex.z);     // <-
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float avg = (col.r + col.g + col.b) / 3;
                col.a = step(_AlphaClip, avg) + _Alpha;
                
                half3 viewDir = normalize(GetWorldSpaceViewDir(i.posWS));
                half3 normal = normalize(i.normal);

                half rim = 1.9 - abs(dot(normal, viewDir));
                rim = pow(rim, _RimPower);

                
                return col * _Color * _Brightness * rim;
            }
            ENDHLSL
        }
        
    }
}