
struct VSInput
{
    float3 pos : POSITION;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

struct PSInput
{
    float4 pos : SV_POSITION;
    float3 worldpos : POSITION;
    float3 camerapos : POSITION1;
    float3 lookat : POSITION2;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

cbuffer ViewConstantsBuffer : register(b0)
{
    float4x4 ViewProjection;
    float3 Camera;
    float3 ViewDirection;
};

cbuffer PerModelConstantBuffer : register(b1)
{
    float4x4 World;
};

struct VSInputUI
{
    float2 pos : POSITION;
    float4 col : COLOR0;
    float2 uv : TEXCOORD0;
};

struct PSInputUI
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
    float2 uv : TEXCOORD0;
};

cbuffer VertexBuffer : register(b0)
{
    float4x4 ProjectionMatrix;
};

struct VSInputMin
{
    float3 pos : POSITION;
    float3 data : NORMAL;
};

struct PSInputMin
{
    float4 pos : SV_POSITION;
    float3 worldpos : POSITION;
    float3 camerapos : POSITION1;
    float3 lookat : POSITION2;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

uint DecodeNormalIndex(float packedFloat)
{
    uint packed = asuint(packedFloat);
    return (packed & 0xFF); // Extract bits 0-7
}

uint DecodeLightValue(float packedFloat)
{
    uint packed = asuint(packedFloat);
    return (packed >> 8) & 0xFF; // Extract bits 8-15
}

uint DecodeUVIndex(float packedFloat)
{
    uint packed = asuint(packedFloat);
    return (packed >> 16) & 0xFF; // Extract bits 16-23
}