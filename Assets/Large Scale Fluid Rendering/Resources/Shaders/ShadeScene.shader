Shader "Custom RP/ShadeScene"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (0.5, 0.5, 0.5)
		_BaseMap("Main Tex", 2D) = "white" {}

		_NormalMap("Normal Map", 2D) = "bump" {}

		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0 
		_Roughness("Roughness", Range(0.0, 1.0)) = 0.5
		_RMAMap("RMA Map", 2D) = "white" {}

		_Specular("Specular", Range(0.0, 1.0)) = 0.5
		_SpecularTint("SpecularTint", Range(0.0, 1.0)) = 0.0

		_Anisotropic("Anisotropic", Range(0.0, 1.0)) = 0.0
		_Subsurface("Subsurface", Range(0.0, 1.0)) = 0.0

		_ShadowBias("Shadow Bias", Range(0.0, 0.1)) = 0.0
		_ShadowStrength("Shadow Strength", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Pass
		{
			Tags
			{
				"LightMode" = "Raster"
			}
			HLSLPROGRAM
			#pragma vertex GeometryPassVert
			#pragma fragment GeometryPassFrag

			#include "Library/Common.hlsl"
			#include "Library/Lighting.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangent: TANGENT;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : VAR_POSITION_WS;
				float4 posLCS : VAR_POSITION_LS;
				float3 normalWS: NORMAL;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			Varyings GeometryPassVert(Attributes input)
			{
				Varyings output;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.positionWS = TransformObjectToWorld(input.positionOS);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.posLCS = getPosLCSWithOffset(output.positionWS, output.normalWS);
				output.uv = input.uv; 
				output.color = input.color;

				float3 tangent = normalize(TransformObjectToWorld(input.tangent));
				float3 bitangent = normalize(cross(output.normalWS, tangent));
				float3 normalMap = normalize(_NormalMap.SampleLevel(sampler_NormalMap, input.uv, 0).rgb * 2 - 1);
				output.normalWS = normalize(tangent * normalMap.x + bitangent * normalMap.y + output.normalWS * normalMap.z);
				return output;
			}

			float4 GeometryPassFrag(Varyings input) : SV_Target
			{
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);				
				return Lighting(normalize(input.normalWS), viewDir, input.positionWS, input.uv, input.posLCS, input.color.xyz);
			}
			ENDHLSL
		}

		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			HLSLPROGRAM
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment

			#include "Library/Common.hlsl"

			float4 ShadowCasterPassVertex(float3 positionOS : POSITION) : SV_POSITION
			{
				return TransformObjectToHClip(positionOS);
			}

			void ShadowCasterPassFragment()
			{

			}
			ENDHLSL
		}

		Pass
		{
			Name "SceneHit"

			HLSLPROGRAM

			#pragma raytracing test

			#include "Library/Common.hlsl"
			#include "Library/RayTracing.hlsl"
			#include "Library/Lighting.hlsl"

			[shader("closesthit")]
			void ClosestHitShader(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
			{
				Vertex intersectionVertex = GetIntersectionVertex(attributeData.barycentrics);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - intersectionVertex.positionWS);
				rayIntersection.positionWS = intersectionVertex.positionWS.xyz;

				float3 bitangent = normalize(cross(intersectionVertex.normalWS, intersectionVertex.tangentWS));
				float3 normalMap = normalize(_NormalMap.SampleLevel(sampler_NormalMap, intersectionVertex.uv, 0).rgb * 2 - 1);
				float3 normalWS = normalize(intersectionVertex.tangentWS * normalMap.x + bitangent * normalMap.y + intersectionVertex.normalWS * normalMap.z);
				float4 posLCS = getPosLCSWithOffset(rayIntersection.positionWS, intersectionVertex.normalWS);

				rayIntersection.color = Lighting(normalWS, viewDir, intersectionVertex.positionWS, intersectionVertex.uv, posLCS, _BaseColor.xyz);
			}

			ENDHLSL
		}
	}

}
