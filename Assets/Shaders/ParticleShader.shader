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

                // Quad corners, -1 to 1
                float2 corners[6] = {
                    float2(-1, -1),
                    float2(-1,  1),
                    float2( 1,  1),
                    float2(-1, -1),
                    float2( 1,  1),
                    float2( 1, -1)
                };
                float2 corner = corners[cornerID];

                // World → Clip space center
                float4 clip = TransformWorldToHClip(float3(p.position, 0.0));

                // Pixel-genaues Offset
                float2 screen = float2(_ScreenParams.x, _ScreenParams.y);
                float radius = _ParticleRadius;

                // Offset in NDC-Space, korrekt skaliert
                float2 offset = corner * radius / screen * 2.0 * clip.w;
                clip.xy += offset;

                o.positionHCS = clip;
                o.uv = corner;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float dist = length(i.uv);
                
                // Feste 1px AA — kein fwidth!
                // radius in Pixeln, 1px Randbreite
                float alpha = smoothstep(1.0, 1.0 - (1.0 / _ParticleRadius), dist);

                if (alpha <= 0.0) discard;

                return float4(1, 1, 1, alpha * 0.75);
            }

            ENDHLSL
        }
    }
}