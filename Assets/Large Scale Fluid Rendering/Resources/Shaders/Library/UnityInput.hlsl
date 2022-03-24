#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
CBUFFER_END

//调用SetupCameraProperties时传入的内置属性
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_CameraInvProjection;
float unity_CameraWorldClipPlanes[6];
float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos;
float4 _ProjectionParams;
float4 _ScreenParams;
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

//需要自己手动传入的属性
float4x4 unity_MatrixIV;
float4x4 unity_MatrixIP;
float4x4 unity_MatrixIVP;
float4x4 glstate_matrix_inv_projection;
float4x4 glstate_matrix_view_projection;
float4x4 glstate_matrix_inv_view_projection;

float4 _WorldSpaceLightPos0;
float4 _WorldSpaceLightDir0;
float4 _LightColor0;
int _IsSpotLight;

#endif