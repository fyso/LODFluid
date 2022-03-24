Shader "Custom RP/Filter"
{
    SubShader
    {
      Pass
      {
        Tags
        {
            "LightMode" = "FlexFilter"
        }
        Blend Off
        ZTest Off
        ZWrite On
        Cull Off

        HLSLPROGRAM
        #include "Library/Common.hlsl"
        #include "Library/FullScreenPassVS.hlsl"
        #pragma fragment FlexFilterPassFrag

        Texture2D _ParticleGBuffer;
        SamplerState sampler_ParticleGBuffer;
        float _ParticlesRadius;
        float _BlurRadiusWorld;

        float FlexFilterPassFrag(Varyings input) : SV_Target
        {
            float depth = _ParticleGBuffer.Sample(sampler_ParticleGBuffer, input.uv).x;

            float blurScale = 0.5f * _ScreenParams.y * abs(glstate_matrix_projection[1][1]);
            float blurDepthFalloff = 5.5;
            float maxBlurRadius = 20.0;

            //discontinuities between different tap counts are visible. to avoid this we 
            //use fractional contributions between #taps = ceil(radius) and floor(radius) 
            float radius = min(maxBlurRadius, blurScale * (_BlurRadiusWorld / -depth));
            float radiusInv = 1.0 / radius;
            float taps = ceil(radius);
            float frac = taps - radius;

            float sum = 0.0;
            float wsum = 0.0;
            float count = 0.0;

            for (float y = -taps; y <= taps; y += 1.0)
            {
                for (float x = -taps; x <= taps; x += 1.0)
                {
                    float sample = _ParticleGBuffer.SampleLevel(sampler_ParticleGBuffer, input.uv + float2(x / _ScreenParams.x, y / _ScreenParams.y), 0).x;

                    //if (sample < -10000.0 * 0.5)
                        //continue;

                    // spatial domain
                    float r1 = length(float2(x, y)) * radiusInv;
                    float w = exp(-(r1 * r1));

                    // range domain (based on depth difference)
                    float r2 = (sample - depth) * blurDepthFalloff;
                    float g = exp(-(r2 * r2));

                    //fractional radius contributions
                    float wBoundary = step(radius, max(abs(x), abs(y)));
                    float wFrac = 1.0 - wBoundary * frac;

                    sum += sample * w * g * wFrac;
                    wsum += w * g * wFrac;
                    count += g * wFrac;
                }
            }

            if (wsum > 0.0)
            {
                sum /= wsum;
            }

            float blend = count / sqr(2.0 * radius + 1.0);
            return lerp(depth, sum, blend);
        }
        ENDHLSL
      }
    }
}