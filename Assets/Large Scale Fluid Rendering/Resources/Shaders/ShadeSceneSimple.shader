Shader "Custom RP/ShadeSceneSimple"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (0.9, 0.9, 0.9)
		_FogColor("Fog Color", Color) = (0.0, 0.0, 0.0, 0.1)
		_ShadowBias("Shadow Bias", Range(0, 0.1)) = 0.0
		_ShadowStrength("Shadow Strength", Range(0, 1)) = 0.5
		_Grid("Grid", Int) = 0
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
			#include "Library/LightingSimple.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : VAR_POSITION_WS;
				float4 posLCS     : VAR_POSITION_LS;
				float3 normalWS   : NORMAL;
				float4 color      : COLOR;
			};

			Varyings GeometryPassVert(Attributes input)
			{
				Varyings output;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.positionWS = TransformObjectToWorld(input.positionOS);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.posLCS = getPosLCSWithOffset(output.positionWS, output.normalWS);
				output.color  = input.color;
				return output;
			}

			float4 GeometryPassFrag(Varyings input) : SV_Target
			{
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
				return LightingSimple(normalize(input.normalWS), viewDir, input.positionWS, input.posLCS, input.color.xyz);
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
			#include "Library/LightingSimple.hlsl"

			[shader("closesthit")]
			void ClosestHitShader(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
			{
				Vertex intersectionVertex = GetIntersectionVertex(attributeData.barycentrics);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - intersectionVertex.positionWS);
				rayIntersection.positionWS = intersectionVertex.positionWS.xyz;
				
				float4 posLCS = getPosLCSWithOffset(rayIntersection.positionWS, intersectionVertex.normalWS);
				rayIntersection.color = LightingSimple(intersectionVertex.normalWS, viewDir, intersectionVertex.positionWS, posLCS, _BaseColor.xyz);
			}

			ENDHLSL
		}
	}

}
