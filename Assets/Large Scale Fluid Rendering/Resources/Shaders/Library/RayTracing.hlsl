#ifndef CUSTOM_RTCOMMON_INCLUDED
#define CUSTOM_RTCOMMON_INCLUDED

#include "UnityRaytracingMeshUtils.cginc"

#define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

RaytracingAccelerationStructure _AccelerationStructure;

struct RayIntersection
{
  float4 color;
  float3 positionWS;
};

struct SDFRayIntersection
{
    float3 positionWS;
    uint hitKind;
};

struct AttributeData
{
  float2 barycentrics;
};

inline float3 GenerateCameraRayWithOffset(float2 offset)
{
	float2 xy = DispatchRaysIndex().xy + offset;
    float4 positionNDC = float4(xy / DispatchRaysDimensions().xy * 2.0f - 1.0f, 0, 1); //Transform pixel from [0,1] to [-1,1]
	float4 positionWS = mul(glstate_matrix_inv_view_projection, positionNDC);
	positionWS.xyz /= positionWS.w;

	return normalize(positionWS.xyz - _WorldSpaceCameraPos.xyz); //世界空间坐标系计算
}

inline float3 GenerateCameraRay()
{
	return GenerateCameraRayWithOffset(0.5f);
}

struct Vertex
{
	float3 positionWS;
	float3 normalWS;
	float3 tangentWS;
	float2 uv;
};

float3 HitWorldPosition()
{
	return WorldRayOrigin() + RayTCurrent() * WorldRayDirection();
}

Vertex GetTriangleVertex(uint vertexIndex)
{
	Vertex vertex;
	vertex.positionWS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
	vertex.normalWS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
	vertex.tangentWS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeTangent);
	vertex.uv = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
	return vertex;
}

Vertex GetIntersectionVertex(float2 baycentrics)
{
	uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
	Vertex v0 = GetTriangleVertex(triangleIndices.x);
	Vertex v1 = GetTriangleVertex(triangleIndices.y);
	Vertex v2 = GetTriangleVertex(triangleIndices.z);

	float3 barycentricCoordinates = float3(1.0 - baycentrics.x - baycentrics.y, baycentrics.x, baycentrics.y);
	float3x3 objectToWorld3x3 = (float3x3)ObjectToWorld3x4();

	Vertex intersectionVertex;
	intersectionVertex.positionWS = HitWorldPosition();
	float3 normalOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normalWS, v1.normalWS, v2.normalWS, barycentricCoordinates);
	intersectionVertex.normalWS = normalize(mul(objectToWorld3x3, normalOS));
	float3 tangentOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.tangentWS, v1.tangentWS, v2.tangentWS, barycentricCoordinates);
	intersectionVertex.tangentWS = normalize(mul(objectToWorld3x3, tangentOS));
	intersectionVertex.uv = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.uv, v1.uv, v2.uv, barycentricCoordinates);

	return intersectionVertex;
}

float getFresnel(float NoI, float IOF)
{
	float R_0 = (1.0f - IOF) / (1.0f + IOF);
	R_0 *= R_0;
	return R_0 + (1.0 - R_0) * pow((1.0 - saturate(NoI)), 5.0f);
}
#endif