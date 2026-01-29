Shader "Custom/WaterDistortion"
{
    Properties
    {
        _DistortionStrength("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionScale("Distortion Scale", Range(0.1, 20)) = 2
        _DistortionSpeed("Distortion Speed", Range(0, 5)) = 1.2
        _ColorTint("Color Tint", Color) = (0.2, 0.6, 0.9, 1)
        _ColorStrength("Color Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "WaterDistortion"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float _DistortionStrength;
                float _DistortionScale;
                float _DistortionSpeed;
                float4 _ColorTint;
                float _ColorStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float2 Hash2(float2 p)
            {
                float2 k = float2(127.1, 311.7);
                float2 s = float2(269.5, 183.3);
                float2 h = float2(dot(p, k), dot(p, s));
                return frac(sin(h) * 43758.5453);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float2 a = Hash2(i + float2(0, 0));
                float2 b = Hash2(i + float2(1, 0));
                float2 c = Hash2(i + float2(0, 1));
                float2 d = Hash2(i + float2(1, 1));

                float2 n0 = lerp(a, b, u.x);
                float2 n1 = lerp(c, d, u.x);
                float2 n = lerp(n0, n1, u.y);
                return n.x;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float t = _Time.y * _DistortionSpeed;
                float2 flow = float2(t * 0.15, t * 0.08);
                float2 p = (uv * _DistortionScale) + flow;

                float n = Noise2D(p);
                float n2 = Noise2D(p + float2(1.7, 3.4));
                float2 distortion = (float2(n, n2) - 0.5) * (2.0 * _DistortionStrength);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                float2 warpedUV = screenUV + distortion;

                float4 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, warpedUV);

                float3 tinted = lerp(sceneColor.rgb, sceneColor.rgb * _ColorTint.rgb, _ColorStrength);
                return float4(tinted, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
