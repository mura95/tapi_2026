Shader "LowPoly/SimpleWater"
{
    Properties
    {
        _WaterNormal("Water Normal", 2D) = "bump" {}
        _NormalScale("Normal Scale", Float) = 0
        _DeepColor("Deep Color", Color) = (0.1, 0.3, 0.5, 0.8)
        _ShalowColor("Shalow Color", Color) = (0.3, 0.7, 0.9, 0.6)
        _WaterDepth("Water Depth", Float) = 0
        _WaterFalloff("Water Falloff", Float) = 0
        _WaterSpecular("Water Specular", Float) = 0
        _WaterSmoothness("Water Smoothness", Float) = 0
        _Distortion("Distortion", Float) = 0.5
        _Foam("Foam", 2D) = "white" {}
        _FoamDepth("Foam Depth", Float) = 0
        _FoamFalloff("Foam Falloff", Float) = 0
        _FoamSpecular("Foam Specular", Float) = 0
        _FoamSmoothness("Foam Smoothness", Float) = 0
        _WavesAmplitude("WavesAmplitude", Float) = 0.01
        _WavesAmount("WavesAmount", Float) = 8.87
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };

            TEXTURE2D(_WaterNormal);
            SAMPLER(sampler_WaterNormal);
            TEXTURE2D(_Foam);
            SAMPLER(sampler_Foam);

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterNormal_ST;
                float4 _Foam_ST;
                float _NormalScale;
                float4 _DeepColor;
                float4 _ShalowColor;
                float _WaterDepth;
                float _WaterFalloff;
                float _WaterSpecular;
                float _WaterSmoothness;
                float _Distortion;
                float _FoamDepth;
                float _FoamFalloff;
                float _FoamSpecular;
                float _FoamSmoothness;
                float _WavesAmplitude;
                float _WavesAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Wave vertex displacement
                float wave = sin((_WavesAmount * input.positionOS.z) + _Time.y) * _WavesAmplitude;
                input.positionOS.xyz += input.normalOS * wave;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Simple fresnel-based color blend (deep vs shallow)
                float fresnel = saturate(1.0 - dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                float4 waterColor = lerp(_ShalowColor, _DeepColor, fresnel);

                // Panning foam texture
                float2 foamUV = input.uv * _Foam_ST.xy + _Foam_ST.zw;
                foamUV += _Time.y * float2(-0.01, 0.01);
                half foam = SAMPLE_TEXTURE2D(_Foam, sampler_Foam, foamUV).r;

                // Add foam at edges (simple depth approximation using fresnel)
                float foamMask = saturate(pow(1.0 - fresnel, _FoamFalloff + 1.0)) * foam;
                waterColor.rgb = lerp(waterColor.rgb, half3(1, 1, 1), foamMask * 0.5);

                // Apply fog
                waterColor.rgb = MixFog(waterColor.rgb, input.fogFactor);

                return waterColor;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
