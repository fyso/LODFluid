#ifndef SPH2D_INCLUDE
#define SPH2D_INCLUDE

#define SPH_THREAD_NUM 512

#define ParticleXGridCountArgumentOffset 0
#define ParticleYGridCountArgumentOffset 1
#define ParticleZGridCountArgumentOffset 2
#define ParticleCountArgumentOffset 4
#define EPSILON 1e-7f
#define PI 3.14159274F

/* compute morton code */
uint expandBits3D(uint v)
{
    v &= 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
    v = (v ^ (v << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
    v = (v ^ (v << 8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
    v = (v ^ (v << 4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
    v = (v ^ (v << 2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
    return v;
}

uint computeMorton3D(uint3 vCellIndex3D)
{
    return (expandBits3D(vCellIndex3D.z) << 2) + (expandBits3D(vCellIndex3D.y) << 1) + expandBits3D(vCellIndex3D.x);
}

float computeCubicKernelW(float vR, float vCubicRadius)
{
    float Result = 0.0f;
    float Q = vR / vCubicRadius;
    float CubicK = 8.0f / (PI * pow(vCubicRadius, 3.0f));
    if (Q <= 1.0f)
    {
        if (Q <= 0.5f)
        {
            float Q2 = Q * Q;
            float Q3 = Q2 * Q;
            Result = CubicK * (6.0f * Q3 - 6.0f * Q2 + 1.0f);
        }
        else
        {
            Result = CubicK * 2.0f * pow(1.0f - Q, 3);
        }
    }
	
    return Result;
}

float3 computeCubicKernelGradW(float3 vR, float vCubicRadius)
{
    float3 Result;
	
    float RLength = length(vR);
    float Q = RLength / vCubicRadius;
    float CubicL = 48.0f / (PI * pow(vCubicRadius, 3.0f));
    if (RLength > 1e-5f && Q <= 1.0f)
    {
        float3 GradQ = vR / (RLength * vCubicRadius);
        if (Q <= 0.5f)
        {
            Result = CubicL * Q * (3.0f * Q - 2.0f) * GradQ;
        }
        else
        {
            float Factor = 1.0f - Q;
            Result = CubicL * Factor * -Factor * GradQ;
        }
    }
    else
    {
        Result = float3(0.0f, 0.0f, 0.0f);
    }
	
    return Result;
}

#endif