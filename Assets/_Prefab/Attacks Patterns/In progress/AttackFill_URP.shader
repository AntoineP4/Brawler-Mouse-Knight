Shader "Custom/AttackFill_URP"
{
    Properties
    {
        _Color ("Color (HDR)", Color) = (2,0.2,0.2,1)
        _Intensity ("Intensity", Range(0, 10)) = 2.5
        _PatternScale ("Pattern Scale", Range(0.1, 30)) = 10.0
        _PatternSpeed ("Pattern Speed", Range(0, 10)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25
        _Alpha ("Alpha", Range(0,1)) = 0.65

        _Hit ("Hit (0-1)", Range(0,1)) = 0
        _HitIntensityBoost ("Hit Intensity Boost", Range(0,6)) = 1.2
        _HitPulseBoost ("Hit Pulse Boost", Range(0,1)) = 0.35
        _HitPatternSpeedBoost ("Hit Pattern Speed Boost", Range(0,10)) = 3.0

        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite [_ZWrite]
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _PatternScale;
                float _PatternSpeed;
                float _PulseSpeed;
                float _PulseAmount;
                float _Alpha;

                float _Hit;
                float _HitIntensityBoost;
                float _HitPulseBoost;
                float _HitPatternSpeedBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                return OUT;
            }

            float Pattern(float3 worldPos, float t, float scale, float speed)
            {
                float2 uv = worldPos.xz * scale + t * speed;
                float stripes = smoothstep(0.45, 0.5, abs(frac(uv.x) - 0.5));
                stripes = 1.0 - stripes;
                return stripes;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float hit = saturate(_Hit);

                float intensity = _Intensity + hit * _HitIntensityBoost;
                float pulseAmt = saturate(_PulseAmount + hit * _HitPulseBoost);
                float patSpeed = _PatternSpeed + hit * _HitPatternSpeedBoost;

                float t = _Time.y;
                float pat = Pattern(IN.positionWS, t, _PatternScale, patSpeed);

                float pulse = (sin(t * _PulseSpeed) * 0.5 + 0.5);
                float pulseMul = lerp(1.0 - pulseAmt, 1.0 + pulseAmt, pulse);

                float3 col = _Color.rgb * intensity * pulseMul;
                col *= lerp(0.75, 1.25, pat);

                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}
