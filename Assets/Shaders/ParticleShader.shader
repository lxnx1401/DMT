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
                float2 offset;      // neu
                float damping;      // neu
                float forceScale;   // neu
            };

            StructuredBuffer<Particle> particles;
            float _ParticleRadius;

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

                float2 corners[6] = {
                    float2(-1, -1),
                    float2(-1,  1),
                    float2( 1,  1),
                    float2(-1, -1),
                    float2( 1,  1),
                    float2( 1, -1)
                };
                float2 corner = corners[cornerID];

                // Offset in World Space anwenden -> skaliert automatisch mit Zoom
                float3 worldPos = float3(p.position + corner * _ParticleRadius, 0.0);
                float4 clip = TransformWorldToHClip(worldPos);

                o.positionHCS = clip;
                o.uv = corner;
                return o;
            }
            float4 frag(Varyings i) : SV_Target
            {
                float dist = length(i.uv);
                float aa = fwidth(dist);
                float alpha = 1.0 - smoothstep(1.0 - aa, 1.0, dist);

                if (alpha <= 0.0) discard;

                return float4(1, 1, 1, alpha * 0.75);
            }

            ENDHLSL
        }
    }
}