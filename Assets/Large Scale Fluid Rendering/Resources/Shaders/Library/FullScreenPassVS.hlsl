#pragma vertex FullScreenPassVertex

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : VAR_SCREEN_UV;
};

Varyings FullScreenPassVertex(uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0);
    output.uv = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0);
    if (_ProjectionParams.x < 0.0)
    {
        output.uv.y = 1.0 - output.uv.y;
    }
    return output;
}