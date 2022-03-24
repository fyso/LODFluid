#pragma vertex GenerateDepthPassVertex
float _ParticlesRadius;
float _ParticlesRadiusScale;
float _DensityISO;

#include "ParticleStruct.hlsl"
StructuredBuffer<Particle> _ParticlesBuffer;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : VAR_SCREEN_UV;
    nointerpolation float3 positionWS : VAR_POSITION3;
    nointerpolation float3 sphereCenterVS : VAR_POSITION_VS;
    nointerpolation float particlesRadius : VAR_RADIUS;
};

Varyings GenerateDepthPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    Varyings output;

    Particle particleData = _ParticlesBuffer[instanceID];

    switch (vertexID)
    {
        case 0:
            output.uv = float2(-1, -1);
            break;

        case 1:
            output.uv = float2(-1, 1);
            break;

        case 2:
            output.uv = float2(1, 1);
            break;

        case 3:
            output.uv = float2(1, -1);
            break;
    }

    output.positionWS = particleData.Position;
    output.sphereCenterVS = TransformWorldToView(particleData.Position);
    output.particlesRadius = _ParticlesRadius;
    output.positionCS = TransformWViewToHClip(output.sphereCenterVS + float3(output.particlesRadius * output.uv, 0.0f));
    return output;
}