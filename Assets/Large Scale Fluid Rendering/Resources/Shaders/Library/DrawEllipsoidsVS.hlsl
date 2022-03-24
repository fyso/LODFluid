#pragma vertex GenerateDepthPassVertex
float _ParticlesRadius;
float _ParticlesRadiusScale;
float _DensityISO;
float _IsScale;

#include "ParticleStruct.hlsl"
StructuredBuffer<Particle> _ParticlesBuffer;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    nointerpolation float4 invQ0 : TEXCOORD1;
    nointerpolation float4 invQ1 : TEXCOORD2;
    nointerpolation float4 invQ2 : TEXCOORD3;
    nointerpolation float4 invQ3 : TEXCOORD4;
};

Varyings Clip()
{
    Varyings output;
    output.positionCS = float4(100, 100, 100, 1);
    return output;
}

Varyings GenerateDepthPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    Varyings output;

    Particle particleData = _ParticlesBuffer[instanceID];
 
    if (_IsScale)
    {
        float scale = 0;
        scale = _ParticlesRadius * 40;
        particleData.AniX.w *= scale;
        particleData.AniY.w *= scale;
        particleData.AniZ.w *= scale;
    }

    float4x4 q;
    q._m00_m10_m20_m30 = float4(particleData.AniX.xyz * particleData.AniX.w, 0.0);
    q._m01_m11_m21_m31 = float4(particleData.AniY.xyz * particleData.AniY.w, 0.0);
    q._m02_m12_m22_m32 = float4(particleData.AniZ.xyz * particleData.AniZ.w, 0.0);
    q._m03_m13_m23_m33 = float4(particleData.Position, 1.0);

	// transforms a normal to parameter space (inverse transpose of (q*modelview)^-T)
    float4x4 invClip = mul(UNITY_MATRIX_VP, q);

	// solve for the right hand bounds in homogenous clip space
    float a1 = DotInvW(invClip[3], invClip[3]);
    float b1 = -2.0f * DotInvW(invClip[0], invClip[3]);
    float c1 = DotInvW(invClip[0], invClip[0]);

    float xmin;
    float xmax;
    solveQuadratic(a1, b1, c1, xmin, xmax);

	// solve for the right hand bounds in homogenous clip space
    float a2 = DotInvW(invClip[3], invClip[3]);
    float b2 = -2.0f * DotInvW(invClip[1], invClip[3]);
    float c2 = DotInvW(invClip[1], invClip[1]);

    float ymin;
    float ymax;
    solveQuadratic(a2, b2, c2, ymin, ymax);

    // construct inverse quadric matrix (used for ray-casting in parameter space)
    float4x4 invq;
    invq._m00_m10_m20_m30 = float4(particleData.AniX.xyz / particleData.AniX.w, 0.0);
    invq._m01_m11_m21_m31 = float4(particleData.AniY.xyz / particleData.AniY.w, 0.0);
    invq._m02_m12_m22_m32 = float4(particleData.AniZ.xyz / particleData.AniZ.w, 0.0);
    invq._m03_m13_m23_m33 = float4(0.0, 0.0, 0.0, 1.0);

    invq = transpose(invq);
    invq._m03_m13_m23_m33 = -(mul(invq, float4(particleData.Position, 1)));

	// transform a point from view space to parameter space
    invq = mul(invq, unity_MatrixIV);

	// pass down
    output.invQ0 = invq._m00_m10_m20_m30;
    output.invQ1 = invq._m01_m11_m21_m31;
    output.invQ2 = invq._m02_m12_m22_m32;
    output.invQ3 = invq._m03_m13_m23_m33;
	
    switch (vertexID)
    {
        case 0:
            output.positionCS = float4(xmin, ymin, 0.5, 1.0);
            break;

        case 1:
            output.positionCS = float4(xmin, ymax, 0.5, 1.0);
            break;

        case 2:
            output.positionCS = float4(xmax, ymax, 0.5, 1.0);
            break;

        case 3:
            output.positionCS = float4(xmax, ymin, 0.5, 1.0);
            break;
    }

    return output; 
}