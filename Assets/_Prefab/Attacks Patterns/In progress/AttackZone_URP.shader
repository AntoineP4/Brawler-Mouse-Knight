Shader "Custom/AttackZone_URP"
{
    Properties
    {
        _BaseColor ("Base Color (RGB) Alpha", Color) = (1,0,0,0.12)
        _EdgeColor ("Edge Color (HDR)", Color) = (2,0.2,0.2,1)
        _EdgePower ("Edge Power", Range(0.5, 8)) = 3.0
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2.0

        _PatternScale ("Pattern Scale", Range(0.1, 20)) = 6.0
        _PatternStrength ("Pattern Strength", Range(0, 1)) = 0.25
        _PatternSpeed ("Pattern Speed", Range(0, 5)) = 0.8

        _DepthFadeDistance ("Depth Fade Distance", Range(0.01, 2)) = 0.35
        _CameraFadeDistance ("Camera Fade Distance", Range(0.01, 2)) = 0.35

        _Hit ("Hit (0-1)", Range(0,1)) = 0
        _HitEdgeBoost ("Hit Edge Boost", Range(0,8)) = 3.0
        _HitPatternStrengthBoost ("Hit Pattern Strength Boost", Range(0,1)) = 0.35
        _HitPatternSpeedBoost ("Hit Pattern Speed Boost", Range(0,10)) = 3.0
        _HitPatternScaleMul ("Hit Pattern Scale Mult", Range(1,6)) = 2.0

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
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _EdgePower;
                float _EdgeIntensity;

                float _PatternScale;
                float _PatternStrength;
                float _PatternSpeed;

                float _DepthFadeDistance;
                float _CameraFadeDistance;

                float _Hit;
                float _HitEdgeBoost;
                float _HitPatternStrengthBoost;
                float _HitPatternSpeedBoost;
                float _HitPatternScaleMul;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Pattern(float3 worldPos, float t, float patternScale, float patternSpeed, float patternStrength)
            {
                float2 uv = worldPos.xz * patternScale;
                uv += t * patternSpeed;

                float lineA = abs(frac(uv.x) - 0.5);
                float lineB = abs(frac(uv.y) - 0.5);
                float grid = smoothstep(0.48, 0.50, max(lineA, lineB));

                float n = Hash(floor(uv));
                float noise = smoothstep(0.2, 0.9, n);

                float pat = saturate(grid * 0.75 + noise * 0.25);
                return lerp(1.0, pat, patternStrength);
            }

            float LinearEyeDepthFromRaw(float rawDepth)
            {
                #if defined(UNITY_REVERSED_Z)
                    rawDepth = 1.0 - rawDepth;
                #endif
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float hit = saturate(_Hit);

                float edgeIntensity = _EdgeIntensity + hit * _HitEdgeBoost;
                float patternStrength = saturate(_PatternStrength + hit * _HitPatternStrengthBoost);
                float patternSpeed = _PatternSpeed + hit * _HitPatternSpeedBoost;
                float patternScale = _PatternScale * lerp(1.0, _HitPatternScaleMul, hit);

                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float fresnel = pow(saturate(1.0 - dot(normalize(IN.normalWS), V)), _EdgePower);

                float t = _Time.y;
                float patMod = Pattern(IN.positionWS, t, patternScale, patternSpeed, patternStrength);

                float baseAlpha = _BaseColor.a * patMod;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;

                float sceneEye = LinearEyeDepthFromRaw(rawDepth);
                float objEye = LinearEyeDepth(IN.screenPos.w, _ZBufferParams);

                float depthGap = max(sceneEye - objEye, 0.0);
                float depthFade = saturate(depthGap / max(_DepthFadeDistance, 1e-4));

                float camDist = distance(_WorldSpaceCameraPos, IN.positionWS);
                float camFade = saturate(camDist / max(_CameraFadeDistance, 1e-4));

                float alpha = baseAlpha * depthFade * camFade;

                float3 col = _BaseColor.rgb;
                col += _EdgeColor.rgb * fresnel * edgeIntensity;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
