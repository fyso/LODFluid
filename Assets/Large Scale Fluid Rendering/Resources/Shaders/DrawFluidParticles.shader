Shader "Custom RP/DrawFluidParticles"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "DrawEllipsoids"
            }

            Blend Off
            ZTest On
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 5.0
            #pragma fragment DrawEllipsoidsFS
            #include "Library/Common.hlsl"
            #include "Library/DrawEllipsoidsVS.hlsl"

            sampler2D _SceneDepth;

            struct Targets
            {
                float depth : SV_Depth;
                float depthVS : SV_Target0;
            };

            Targets DrawEllipsoidsFS(Varyings input)
            {
                // transform from view space to parameter space
                //column_major
                float4x4 invQuadric;
                invQuadric._m00_m10_m20_m30 = input.invQ0;
                invQuadric._m01_m11_m21_m31 = input.invQ1;
                invQuadric._m02_m12_m22_m32 = input.invQ2;
                invQuadric._m03_m13_m23_m33 = input.invQ3;

                float4 ndcPos = float4(input.positionCS.x * (1.0 / _ScreenParams.x) * 2.0f - 1.0f, input.positionCS.y * (1.0 / _ScreenParams.y) * 2.0 - 1.0, 0.0f, 1.0);
                float4 viewDir = mul(unity_MatrixIP, ndcPos);

                // ray to parameter space
                float4 dir = mul(invQuadric, float4(viewDir.xyz, 0.0));
                float4 origin = invQuadric._m03_m13_m23_m33;

                // set up quadratric equation
                float a = sqr(dir.x) + sqr(dir.y) + sqr(dir.z);
                float b = dir.x * origin.x + dir.y * origin.y + dir.z * origin.z - dir.w * origin.w;
                float c = sqr(origin.x) + sqr(origin.y) + sqr(origin.z) - sqr(origin.w);

                float minT;
                float maxT;

                if (!solveQuadratic(a, 2.0 * b, c, minT, maxT))
                {
                    discard;
                }
                float3 eyePos = viewDir.xyz * minT;

                ndcPos = TransformWViewToHClip(float4(eyePos, 1.0));
                ndcPos.z /= ndcPos.w;

                float sceneDepth = tex2D(_SceneDepth, GetUVFromCS(ndcPos)).x;
                if (ndcPos.z < sceneDepth) discard;

                Targets output;
                output.depth = ndcPos.z;
                output.depthVS = eyePos.z;
                return output;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DrawSphere"
            }

            Blend Off
            ZTest On
            ZWrite On

            HLSLPROGRAM
            #pragma target 5.0
            #pragma fragment DrawSpriteFS
            #include "Library/Common.hlsl"
            #include "Library/DrawSphereVS.hlsl"

            sampler2D _SceneDepth;

            struct Targets
            {
                float depth : SV_Depth;
                float depthVS : SV_Target0;
            };

            Targets DrawSpriteFS(Varyings input)
            {
                float3 normalVS;
                normalVS.xy = input.uv;

                float xy_PlaneProj = dot(normalVS.xy, normalVS.xy);
                if (xy_PlaneProj > 1.0f) discard;
                normalVS.z = sqrt(1.0f - xy_PlaneProj);

                float3 positionVS = input.sphereCenterVS.xyz + normalVS * input.particlesRadius;
                if (-positionVS.z <= unity_CameraWorldClipPlanes[4])
                    positionVS.z = -unity_CameraWorldClipPlanes[4];

                float4 positionCS = TransformWViewToHClip(positionVS);
                float fluidDepth = positionCS.z / positionCS.w;
                float sceneDepth = tex2D(_SceneDepth, GetUVFromCS(positionCS)).x;
                if (fluidDepth < sceneDepth) discard;

                Targets output;
                output.depth = fluidDepth;
                output.depthVS = positionVS.z;
                return output;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "Shadow"
            }

            Blend Off
            ZTest On
            ZWrite On

            HLSLPROGRAM
            #pragma target 5.0
            #pragma fragment DrawSpriteFS
            #include "Library/Common.hlsl"
            #include "Library/DrawSphereVS.hlsl"

            void DrawSpriteFS(Varyings input)
            {
                float3 normalVS;
                normalVS.xy = input.uv;

                float xy_PlaneProj = dot(normalVS.xy, normalVS.xy);
                if (xy_PlaneProj > 1) discard;
            }

            ENDHLSL
        }
    }
}