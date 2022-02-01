#ifndef SPH2D_INCLUDE
#define SPH2D_INCLUDE

#define SPH_THREAD_NUM 512
#pragma shader_feature _2DWorld

#ifdef _2DWorld
#define VectorFloat float2
#define VectorInt int2
#define VectorUInt uint2
#else
#define VectorFloat float3
#define VectorInt int3
#define VectorUInt uint3
#endif

#define ParticleXGridCountArgumentOffset 0
#define ParticleYGridCountArgumentOffset 1
#define ParticleZGridCountArgumentOffset 2
#define ParticleCountArgumentOffset 4

#endif