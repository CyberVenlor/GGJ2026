Shader "Custom/FlameBillboard"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1.0, 0.55, 0.15, 1.0)
        _MidColor("Mid Color", Color) = (1.0, 0.35, 0.05, 1.0)
        _TipColor("Tip Color", Color) = (1.0, 0.85, 0.4, 1.0)
        _Intensity("Intensity", Range(0.5, 6.0)) = 2.5
        _Alpha("Alpha", Range(0.0, 1.0)) = 0.9
        _Scale("Noise Scale", Range(0.5, 12.0)) = 5.5
        _Speed("Scroll Speed", Range(0.0, 6.0)) = 2.0
        _Turbulence("Turbulence", Range(0.0, 1.2)) = 0.6
        _Distort("Distortion", Range(0.0, 0.6)) = 0.25
        _Shape("Shape Power", Range(0.5, 4.0)) = 1.6
        _EdgeSoftness("Edge Softness", Range(0.02, 0.5)) = 0.18
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
            Name "Flame"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MidColor;
                float4 _TipColor;
                float _Intensity;
                float _Alpha;
                float _Scale;
                float _Speed;
                float _Turbulence;
                float _Distort;
                float _Shape;
                float _EdgeSoftness;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0.0, 0.0));
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float n0 = lerp(a, b, u.x);
                float n1 = lerp(c, d, u.x);
                return lerp(n0, n1, u.y);
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 4; i++)
                {
                    value += Noise2D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return value;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = _Time.y * _Speed;

                float2 p = uv * _Scale + float2(0.0, t);
                float n1 = FBM(p);
                float n2 = FBM(p * 2.0 + float2(t * 1.3, t * 0.7));
                float noise = lerp(n1, n2, 0.5);

                float2 warp = float2(noise - 0.5, FBM(p + 4.3) - 0.5) * _Distort;
                uv.x += warp.x * (1.0 - uv.y);
                uv.y += warp.y * 0.06;

                float height = saturate(1.0 - uv.y);
                float shape = pow(height, _Shape);
                float flame = shape - noise * _Turbulence;
                float mask = smoothstep(0.0, _EdgeSoftness, flame);

                float h = saturate(height);
                float3 color = lerp(_TipColor.rgb, _MidColor.rgb, smoothstep(0.0, 0.5, h));
                color = lerp(color, _BaseColor.rgb, smoothstep(0.5, 1.0, h));

                float3 emissive = color * _Intensity;
                float alpha = mask * _Alpha;

                return float4(emissive, alpha);
            }
            ENDHLSL
        }
    }
}
