using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SParticle
{
    public Vector3 Position;
    public float Density;
    public Vector4 AniX;
    public Vector4 AniY;
    public Vector4 AniZ;
    public float Speed;
};
public struct SDiffuseParticle
{
    public Vector4 Position;
    public Vector3 Velocity;
};
public struct SMeshVertexAttribute
{
    public Vector3 Position;
    public Color Color;
};