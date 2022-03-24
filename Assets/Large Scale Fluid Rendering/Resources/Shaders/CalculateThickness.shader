Shader "Custom RP/CalculateThickness"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "ThicknessCreater"
            }

            Blend One One
            ZTest Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 5.0
            #pragma fragment GenerateThicknessPassFrag 
            #pragma vertex GenerateDepthPassVertex
            #include "Library/Common.hlsl"
            #include "Library/DrawSphereVS.hlsl"

			Texture2D _SceneDepth;
			SamplerState sampler_SceneDepth;

			float GenerateThicknessPassFrag(Varyings input) : SV_Target
            {
                float4 centerPositionCS = TransformWViewToHClip(input.sphereCenterVS.xyz);
                float sceneDepth = _SceneDepth.Sample(sampler_SceneDepth, GetUVFromCS(centerPositionCS)).x;
                #if UNITY_REVERSED_Z
                    sceneDepth = 1 - sceneDepth;
                #endif
                if (centerPositionCS.z / centerPositionCS.w > sceneDepth) discard;

                float3 normalVS;
                normalVS.xy = input.uv;

                float xy_PlaneProj = dot(normalVS.xy, normalVS.xy);
                if (xy_PlaneProj > 1.0f) discard;
                normalVS.z = sqrt(1.0f - xy_PlaneProj);

                return normalVS.z * 0.005;
            }

            ENDHLSL
        }
    }
}