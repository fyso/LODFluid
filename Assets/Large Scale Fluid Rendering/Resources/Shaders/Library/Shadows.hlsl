#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

SamplerComparisonState sampler_linear_clamp_compare;
Texture2D _Shadow;
int _ShadowType;
matrix _DirectionalShadowMatrices;
float _ShadowBias;
float _ShadowStrength;

float SampleShadow(Texture2D shadowMap, float3 positionCS)
{
    return SAMPLE_TEXTURE2D_SHADOW(shadowMap, sampler_linear_clamp_compare, positionCS);
}

static float2 shadowTaps[8] = {
    { -0.326211989, -0.405809999 },
    { -0.840143979, -0.0735799968 },
    { -0.695913970, 0.457136989 },
    { -0.203345001, 0.620715976 },
    { 0.962339997, -0.194983006 },
    { 0.473434001, -0.480026007 },
    { 0.519456029, 0.767022014 },
    { 0.185461000, -0.893123984 }
};


float SampleSoftShadow(float3 positionCS)
{
    float shadow = 0;
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        shadow += SampleShadow(_Shadow, float3(positionCS.xy + shadowTaps[i] * 0.002, positionCS.z));
    }
    return shadow / 8;
}

float getShadow(float3 posLCS)
{
    if (_ShadowType == 0) return 1;

    float3 positionCS = posLCS;
    positionCS = (positionCS + 1) / 2.0;
    positionCS.z = 1 - positionCS.z;

    float shadow = 1;
    if (_ShadowType == 1)
    {
        shadow = SampleShadow(_Shadow, positionCS);
    }
    else if (_ShadowType == 2)
    {
        shadow = SampleSoftShadow(positionCS);
    }

    return lerp(1, shadow, _ShadowStrength);
}

float4 getPosLCSWithOffset(float3 positionWS, float3 normalWS)
{
    return mul(_DirectionalShadowMatrices, float4(positionWS + normalWS * _ShadowBias, 1.0));
}

#endif