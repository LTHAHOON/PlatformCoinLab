Shader "CoinGame/Toon Grass"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _LightTint("Light Tint", Color) = (1,1,1,1)
        _MiddleTint("Middle Tint", Color) = (0.75,0.8,0.7,1)
        _ShadowTint("Shadow Tint", Color) = (0.4,0.5,0.4,1)
        _DiffuseThresholds("Diffuse Bands", Vector) = (-0.2,-0.1,0.45,0.55)
        _ShadowThresholds("Shadow Bands", Vector) = (0.1,0.2,0,0)
        _RimColor("Rim Color", Color) = (0.02,0.04,0.02,1)
        _RimThresholds("Rim Thresholds", Vector) = (0.75,0.85,0,0)
        _RimExtension("Rim Extension", Float) = 0
        _SpecularThresholds("Specular Thresholds", Vector) = (0.1,0.2,0,0)
        _SpecularBoost("Specular Boost", Float) = 1
        _GIStrength("GI Strength", Range(0,1)) = 0.5
        _GrassOpacity("Grass Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half3 _LightTint;
            half3 _MiddleTint;
            half3 _ShadowTint;
            float4 _DiffuseThresholds;
            float2 _ShadowThresholds;
            half3 _RimColor;
            float2 _RimThresholds;
            float _RimExtension;
            float2 _SpecularThresholds;
            float _SpecularBoost;
            float _GIStrength;
            float _GrassOpacity;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half3 vertexSH : TEXCOORD3;
            float2 staticLightmapUV : TEXCOORD4;
            float2 dynamicLightmapUV : TEXCOORD5;
            float fog : TEXCOORD6;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // Reuse the installed package's BRDF and toon band calculations.
        #define _USE_SECOND_THRESHOLD
        #define _RECEIVE_SHADOWS_ON
        #include "Assets/Toon Shaders Pro/Shaders/Toon/ToonFunctions.hlsl"

        v2f GrassVertex(Attributes input)
        {
            v2f output = (v2f)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionWS = TransformObjectToWorld(input.positionOS);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.vertexSH = SampleSH(output.normalWS);
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }

        void GrassDither(float2 screenPosition)
        {
            clip(_GrassOpacity - InterleavedGradientNoise(screenPosition, 0));
        }

        half4 GrassFragment(v2f input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            GrassDither(input.positionCS.xy);
            half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
            half3 normal = normalize(input.normalWS);
            half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            BRDFData brdf;
            ToonBRDFData(color.rgb, 0, half3(0,0,0), 0, color.a, brdf);
            Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half3 lighting = CalculateToonLighting(brdf, light, normal, viewDirection, 0);
            lighting += SampleSH(normal) * color.rgb * _GIStrength;
            lighting = MixFog(lighting, input.fog);
            return half4(lighting, 1);
        }
        ENDHLSL

        // Grass deliberately has no depth, depth-normal or shadow-caster pass.
        Pass
        {
            Name "GrassForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassVertex
            #pragma fragment GrassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }
    }
}
