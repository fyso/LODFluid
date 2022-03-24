Shader "Custom RP/GenerateNoramal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [Toggle(_RECONSTRUCT_HIGH_QUALITY_NORMAL)] _ReconstructHighQulityNormalToggle("Reconstruct High-Qulity Normal", Float) = 1
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "GenerateNoramalFromVS"
            }
            Blend Off
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #include "Library/Common.hlsl"
            #include "Library/FullScreenPassVS.hlsl"
            #pragma fragment CalculateNormalPassFrag
            #pragma shader_feature _RECONSTRUCT_HIGH_QUALITY_NORMAL

            Texture2D _DepthTex;
            SamplerState sampler_DepthTex;

            float4 CalculateNormalPassFrag(Varyings input) : SV_TARGET
            {
                float2 texCoord = input.uv;
                float depthVS = _DepthTex.Sample(sampler_DepthTex, texCoord).x;

                if (depthVS >= 0.0)
                    discard;


                float depth = GetDepthCSFromDepthVS(depthVS);
                float3 positionVS = GetPositionVSFromDepth(texCoord, depth);
                float2 texelSize = 1. / _ScreenParams.xy;

                #if _RECONSTRUCT_HIGH_QUALITY_NORMAL
                    float2 texCoordRight = texCoord + float2(texelSize.x, 0);
                    float2 texCoordLeft = texCoord + float2(-texelSize.x, 0);
                    float2 texCoordUp = texCoord + float2(0, texelSize.y);
                    float2 texCoordDown = texCoord + float2(0, -texelSize.y);

                    float depthRightVS = _DepthTex.Sample(sampler_DepthTex, texCoordRight).x;
                    float3 positionRightVS = GetPositionVSFromDepthVS(texCoordRight, depthRightVS);

                    float depthLeftVS = _DepthTex.Sample(sampler_DepthTex, texCoordLeft).x;
                    float3 positionLeftVS = GetPositionVSFromDepthVS(texCoordLeft, depthLeftVS);

                    float depthUpVS = _DepthTex.Sample(sampler_DepthTex, texCoordUp).x;
                    float3 positionUpVS = GetPositionVSFromDepthVS(texCoordUp, depthUpVS);

                    float depthDownVS = _DepthTex.Sample(sampler_DepthTex, texCoordDown).x;
                    float3 positionDownVS = GetPositionVSFromDepthVS(texCoordDown, depthDownVS);

                    float3 zl = positionVS - positionLeftVS;
                    float3 zr = positionRightVS - positionVS;
                    float3 zt = positionUpVS - positionVS;
                    float3 zb = positionVS - positionDownVS;

                    float3 dx = zl;
                    float3 dy = zt;

                    if (abs(zr.z) < abs(zl.z))
                        dx = zr;

                    if (abs(zb.z) < abs(zt.z))
                        dy = zb;

                    float3 normalVS = normalize(cross(dx, dy));
                    float3 normalWS = mul(unity_MatrixIV, float4(normalVS, 0));
                #else
                    float3 normalVS = normalize(cross(ddx(positionVS), ddy(positionVS)));
                    float3 normalWS = mul(unity_MatrixIV, float4(normalVS, 0));
                #endif

                normalWS = normalize(normalWS);
                return float4(normalWS, depth);
            }

            ENDHLSL
        }
    }
}