#ifndef PARTICLESTRUCT_INCLUDED
#define PARTICLESTRUCT_INCLUDED

struct Particle
{
    float3 Position;
    float Density;
    float4 AniX;
    float4 AniY;
    float4 AniZ;
    float Speed;
};

struct DiffuseParticle
{
    float4 Position; //lifetime in w
    float3 Velocity;
};
#endif
