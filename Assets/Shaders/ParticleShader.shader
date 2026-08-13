Shader "Custom/ParticleShader"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Cull Off
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Particle
            {
                float2 position;
                float2 velocity;
                float2 offset;
                float damping;
                float forceScale;
                float alive;
            };

            StructuredBuffer<Particle> particles;

            float _ParticleRadius;
            float4 _TintColor;

            float _StartHue;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;

                uint particleID = vertexID / 6;
                uint cornerID   = vertexID % 6;

                Particle p = particles[particleID];

                if (p.alive < 0.5)
                {
                    o.positionHCS = float4(0,0,0,0);
                    o.uv = 0;
                    return o;
                }

                float2 corners[6] = {
                    float2(-1, -1),
                    float2(-1,  1),
                    float2( 1,  1),
                    float2(-1, -1),
                    float2( 1,  1),
                    float2( 1, -1)
                };

                float2 corner = corners[cornerID];

                float3 worldPos = float3(
                    p.position + corner * _ParticleRadius,
                    0.0
                );

                float4 clip = TransformWorldToHClip(worldPos);

                o.positionHCS = clip;
                o.uv = corner;

                return o;
            }

            float3 HueToRGB(float h)
            {
                float3 rgb = abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
                return saturate(rgb - 1.0);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float dist = length(i.uv);
                float aa = fwidth(dist);
                float alpha = 1.0 - smoothstep(1.0 - aa, 1.0, dist);

                if (alpha <= 0.0)
                    discard;

                // Hue läuft kontinuierlich von 0 -> 1 und beginnt danach wieder von vorne.
                float hue = _StartHue;

                float3 rainbowColor = HueToRGB(hue);

                // _TintColor beeinflusst weiterhin Helligkeit/Alpha.
                float3 finalColor = rainbowColor * _TintColor.rgb;

                return float4(
                    finalColor,
                    alpha * _TintColor.a
                );
            }

            ENDHLSL
        }
    }
}