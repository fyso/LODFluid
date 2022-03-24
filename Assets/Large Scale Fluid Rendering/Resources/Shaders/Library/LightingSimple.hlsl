#ifndef CUSTOM_LGIHTING_INCLUDED
#define CUSTOM_LGIHTING_INCLUDED

float4 _BaseColor;
float4 _FogColor;
int _Grid;
int _AmbientStrength;

#include "Shadows.hlsl"

float2 bump(float2 x)
{
    return (floor((x) / 2) + 2.f * max(((x) / 2) - floor((x) / 2) - .5f, 0.f));
}

float checker(float2 uv)
{
    float width = 0.01;
    float2 p0 = uv - 0.5 * width;
    float2 p1 = uv + 0.5 * width;

    float2 i = (bump(p1) - bump(p0)) / width;
    return i.x * i.y + (1 - i.x) * (1 - i.y);
}

float4 LightingSimple(float3 N, float3 V, float3 P, float4 PosLCS, float3 vVertexColor)
{
    float3 posLCS = PosLCS.xyz / PosLCS.w;
    float NdotL = dot(N, _WorldSpaceLightDir0.xyz);

	// direct light term
    float3 color = vVertexColor.xyz;

    if (_Grid && (N.y > 0.995))
    {
        color *= 1.0 - 0.25 * checker(float2(P.x, P.z));
    }
    else if (_Grid && abs(N.z) > 0.995)
    {
        color *= 1.0 - 0.25 * checker(float2(P.y, P.x));
    }
    else if (_Grid && abs(N.x) > 0.995)
    {
        color *= 1.0 - 0.25 * checker(float2(P.z, P.y));
    }

    float attenuation = 1;
    if (_IsSpotLight)
    {
        attenuation = max(smoothstep(1, 0.1, dot(posLCS.xy, posLCS.xy)), 0.05);
    }

    float3 diffuse = color * _LightColor0.rgb * max(0.0, NdotL) * attenuation;

	// wrap ambient term aligned with light dir
	float3 light = float3(0.03, 0.025, 0.025) * 1.5;
	float3 dark = float3(0.025, 0.025, 0.03);
    float3 ambient = _AmbientStrength * color * lerp(dark, light, NdotL * float3(0.5, 0.5, 1.0) + float3(0.5, 0.5, 0.0)) * attenuation;
    float3 fog = lerp(_FogColor.xyz, (diffuse + ambient) * getShadow(posLCS), exp(mul(UNITY_MATRIX_V, float4(P, 1)).z * _FogColor.w));

	const float tmp = 1.0 / 2.2;
    return float4(pow(abs(fog), float3(tmp, tmp, tmp)), 1.0);
}

#endif