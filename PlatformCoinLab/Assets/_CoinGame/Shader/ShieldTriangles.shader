Shader "CoinGame/VFX/ShieldTriangles"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.02, 0.25, 1, 1)
        _EdgeColor ("Edge Color", Color) = (0.2, 0.8, 1, 1)
        _CellDensity ("Cell Density", Float) = 9
        _CellGap ("Cell Gap", Range(0.005, 0.15)) = 0.035
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseAmplitude ("Pulse Amplitude", Range(0, 1)) = 0.35
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Opacity ("Opacity", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EdgeColor;
            float _CellDensity;
            float _CellGap;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _RimPower;
            float _Opacity;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 cell = floor(input.uv * _CellDensity);
                float phase = Hash21(cell) * 6.2831853;
                float pulse = sin(_Time.y * _PulseSpeed + phase) * 0.5 + 0.5;
                input.positionOS.xyz += input.normalOS * ((pulse - 0.5) * _PulseAmplitude * 0.035);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 grid = input.uv * _CellDensity;
                float2 cell = floor(grid);
                float2 f = frac(grid);
                bool upper = f.x + f.y > 1.0;
                float2 triPoint = upper ? 1.0 - f : f;
                float edgeDistance = min(min(triPoint.x, triPoint.y), 1.0 - triPoint.x - triPoint.y);
                float edge = 1.0 - smoothstep(_CellGap, _CellGap + 0.035, edgeDistance);
                float pulse = sin(_Time.y * _PulseSpeed + Hash21(cell) * 6.2831853) * 0.5 + 0.5;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                float intensity = lerp(0.35, 1.0, pulse);
                float3 color = _BaseColor.rgb * intensity + _EdgeColor.rgb * (edge * 2.5 + rim * 1.4);
                float alpha = _Opacity * saturate(0.16 + edge * 0.62 + rim * 0.38);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
