#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
#pragma enable_d3d11_debug_symbols

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

//------———————-----深度转换为位置————————————————
float3 GetPositionVSFromDepth(float2 texCoord, float depth, bool isDiscard = false)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif
    if (isDiscard && depth >= 1.0)
    {
        discard;
        return 0;
    }

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionVS = mul(unity_MatrixIP, positionNDC);
    return positionVS.xyz / positionVS.w;
}

float3 GetPositionVSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord, bool isDiscard = false)
{
    float depth = depthTex.Sample(sampler_MainTex, texCoord).x;
    return GetPositionVSFromDepth(texCoord, depth, isDiscard);
}

float3 GetPositionWSFromDepth(float2 texCoord, float depth, bool isDiscard)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif
    if (isDiscard && depth >= 1.0)
    {
        discard;
        return 0;
    }

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionWS = mul(unity_MatrixIVP, positionNDC);
    return positionWS.xyz / positionWS.w;
}

float3 GetPositionWSFromDepth(float2 texCoord, float depth)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionWS = mul(unity_MatrixIVP, positionNDC);
    return positionWS.xyz / positionWS.w;
}

float3 GetPositionWSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord, bool isDiscard = false)
{
    float depth = depthTex.Sample(sampler_MainTex, texCoord).x;
    return GetPositionWSFromDepth(texCoord, depth, isDiscard);
}

//------———————-----获取各种空间的深度————————————————
float GetDepthVS(float depth)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif

    depth = depth * 2.0f - 1.0f;
    return unity_MatrixIP[2][3] / (unity_MatrixIP[3][2] * depth + unity_MatrixIP[3][3]);
}

float GetDepthVSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord)
{
    float depth = depthTex.SampleLevel(sampler_MainTex, texCoord, 0).x;
    return GetDepthVS(depth);
}

float GetDepthCSFromVS(float depthVS)
{
    return (UNITY_MATRIX_P[2][2] * depthVS + UNITY_MATRIX_P[2][3]) / (UNITY_MATRIX_P[3][2] * depthVS);
}

float GetDepthCSFromDepthVS(float DepthVS)
{
    return (glstate_matrix_projection[2][2] * DepthVS + glstate_matrix_projection[2][3])
            / (glstate_matrix_projection[3][2] * DepthVS);
}

float3 GetPositionVSFromDepthVS(float2 texCoord, float DepthVS)
{
    if (DepthVS == 0)
        return 1000000000;
    else return GetPositionVSFromDepth(texCoord, GetDepthCSFromDepthVS(DepthVS));
}



//------———————-----工具函数————————————————
float2 GetUVFromCS(float4 positionCS)
{
    float2 uv = 0.5f * positionCS.xy / positionCS.w + 0.5f;
    if (_ProjectionParams.x < 0.0) uv.y = 1.0 - uv.y;
    return uv;
}

float Rand(float2 texCoord)
{
    return frac(sin(dot(texCoord.xy, float2(12.9898, 78.233))) * 43758.5453);
}

#define MAX_FILTERSIZE_1D 64
#define MAX_FILTERSIZE_2D 32

float CalculateGaussianDistributionWeight(float delta, float sigma)
{
    if (sigma == 0)
        return 1;
    float ratio = delta / sigma;
    return exp(-0.5 * ratio * ratio);
}

float CalculateGaussianDistributionWeight2D(float2 delta, float sigma)
{
    if (sigma == 0)
        return 1;
    float two_Delata = dot(delta, delta);
    float two_Sigma = sigma * sigma;
    return exp(-0.5 * two_Delata / two_Sigma);
}

float CalculateGaussianDistributionWeight3D(float3 delta, float sigma)
{
    if (sigma == 0)
        return 1;
    float two_Delata = dot(delta, delta);
    float two_Sigma = sigma * sigma;
    return exp(-0.5 * two_Delata / two_Sigma);
}

void ModifiedGaussianFilter(inout float sampleDepth, inout float weight, inout float upper, inout float lower, float lower_clamp, float threshold)
{
    if (sampleDepth > upper)
    {
        weight = 0;
    }
    else if (sampleDepth < lower)
    {
        sampleDepth = lower_clamp;
    }
    else
    {
        float newUpper = sampleDepth + threshold;
        upper = max(upper, newUpper);
        float newLower = sampleDepth - threshold;
        lower = max(lower, newLower);
    }
}





float Sign(float x)
{
    return x < 0.0 ? -1.0 : 1.0;
}

bool solveQuadratic(float a, float b, float c, out float minT, out float maxT)
{
    if (a == 0.0 && b == 0.0)
    {
        minT = maxT = 0.0;
        return false;
    }

    float discriminant = b * b - 4.0 * a * c;

    if (discriminant < 0.0)
    {
        return false;
    }

    float t = -0.5 * (b + Sign(b) * sqrt(discriminant));
    minT = t / a;
    maxT = c / t;

    if (minT > maxT)
    {
        float tmp = minT;
        minT = maxT;
        maxT = tmp;
    }

    return true;
}

float DotInvW(float4 a, float4 b)
{
    return a.x * b.x + a.y * b.y + a.z * b.z - a.w * b.w;
}

float sqr(float x)
{
    return x * x;
}

#endif
