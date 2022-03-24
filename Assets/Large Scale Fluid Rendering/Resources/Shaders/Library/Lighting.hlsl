#ifndef CUSTOM_LGIHTING_INCLUDED
#define CUSTOM_LGIHTING_INCLUDED

float4 _BaseColor;
Texture2D _BaseMap;
SamplerState sampler_BaseMap;

Texture2D _NormalMap;
SamplerState sampler_NormalMap;

float _Metallic;
float _Roughness;
Texture2D _RMAMap;
SamplerState sampler_RMAMap;

float _Specular;
float _SpecularTint;

float _Anisotropic;
float _Subsurface;
float _AmbientStrength;

#include "Shadows.hlsl"

float SchlickFresnel(float u)
{
	float m = clamp(1 - u, 0, 1);
	float m2 = m * m;
	return m2 * m2 * m; // pow(m,5)
}

float GTR1(float NdotH, float a)
{
	if (a >= 1) return 1 / PI;
	float a2 = a * a;
	float t = 1 + (a2 - 1) * NdotH * NdotH;
	return (a2 - 1) / (PI * log(a2) * t);
}

float GTR2(float NdotH, float a)
{
	float a2 = a * a;
	float t = 1 + (a2 - 1) * NdotH * NdotH;
	return a2 / (PI * t * t);
}

float smithG_GGX(float NdotV, float alphaG)
{
	float a = alphaG * alphaG;
	float b = NdotV * NdotV;
	return 1 / (NdotV + sqrt(a + b - a * b));
}

float3 mon2lin(float3 x)
{
	return float3(pow(x[0], 2.2), pow(x[1], 2.2), pow(x[2], 2.2));
}

float4 Lighting(float3 N, float3 V, float3 P, float2 uv, float4 PosLCS, float3 vVertexColor)
{
    float3 posLCS = PosLCS.xyz / PosLCS.w;

    float3 L = _WorldSpaceLightDir0.xyz;
	float NdotL = dot(N, L);
	float NdotV = dot(N, V);
    if (NdotL < 0) return float4(0, 0, 0, 1);

	float3 H = normalize(L + V);
	float NdotH = dot(N, H);
	float LdotH = dot(L, H);

	float3 Cdlin = mon2lin(vVertexColor * _BaseMap.SampleLevel(sampler_BaseMap, uv, 0).rgb);
	float Cdlum = .3 * Cdlin[0] + .6 * Cdlin[1] + .1 * Cdlin[2]; // luminance approx.

	float3 rma = _RMAMap.SampleLevel(sampler_RMAMap, uv, 0);
	float matellic = _Metallic * rma.g;
	float3 Ctint = Cdlum > 0 ? Cdlin / Cdlum : float3(1, 1, 1); // normalize lum. to isolate hue+sat
	float3 Cspec0 = lerp(_Specular * .08 * lerp(float3(1, 1, 1), Ctint, _SpecularTint), Cdlin, matellic);

	// Diffuse fresnel - go from 1 at normal incidence to .5 at grazing
	// and lerp in diffuse retro-reflection based on roughness
	float FL = SchlickFresnel(NdotL), FV = SchlickFresnel(NdotV);
	float roughness = _Roughness * rma.r;
	float Fd90 = 0.5 + 2 * LdotH * LdotH * roughness;
	float Fd = lerp(1.0, Fd90, FL) * lerp(1.0, Fd90, FV);

	// Based on Hanrahan-Krueger brdf approximation of isotroPIc bssrdf
	// 1.25 scale is used to (roughly) preserve albedo
	// Fss90 used to "flatten" retroreflection based on roughness
	float Fss90 = LdotH * LdotH * roughness;
	float Fss = lerp(1.0, Fss90, FL) * lerp(1.0, Fss90, FV);
	float ss = 1.25 * (Fss * (1 / (NdotL + NdotV) - .5) + .5);

	// specular
    float aspect = sqrt(1 - _Anisotropic * .9);
	float Ds = GTR2(NdotH, max(.001, roughness));
	float FH = SchlickFresnel(LdotH);
	float3 Fs = lerp(Cspec0, float3(1, 1, 1), FH);
	float Gs;
	Gs = smithG_GGX(NdotL, roughness);
	Gs *= smithG_GGX(NdotV, roughness);

	float3 brdf = ((1 / PI) * lerp(Fd, ss, _Subsurface) * Cdlin) * (1 - matellic) + Gs * Fs * Ds;
	
    float attenuation = 1;
    if (_IsSpotLight)
    {
        attenuation = max(smoothstep(1, 0.1, dot(posLCS.xy, posLCS.xy)), 0.05);
    }

    float3 light = float3(0.03, 0.025, 0.025) * 1.5;
    float3 dark = float3(0.025, 0.025, 0.03);
    float3 ambient = _AmbientStrength * vVertexColor * lerp(dark, light, NdotL * float3(0.5, 0.5, 1.0) + float3(0.5, 0.5, 0.0)) * attenuation;

    return float4(brdf * _LightColor0.rgb * PI * NdotL * rma.b * attenuation + ambient, 1) * getShadow(posLCS);
}

#endif