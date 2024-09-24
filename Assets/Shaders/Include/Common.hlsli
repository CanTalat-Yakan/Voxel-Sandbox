
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
    float2 data : TEXCOORD0;
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

// Unpack a float into a float3 (X, Y, Z) where X and Z get 8 bits, Y gets 16 bits
float3 UnpackFloatToVector3(float packedFloat)
{
    uint packed = asuint(packedFloat);

    // Extract the 8 bits for X
    float X = (packed >> 24) & 0xFF;

    // Extract the 16 bits for Y
    float Y = (packed >> 8) & 0xFFFF;

    // Extract the 8 bits for Z
    float Z = packed & 0xFF;

    return float3(X, Y, Z);
}

// Unpack a float into 4 bytes
int4 UnpackFloatToBytes(float packedFloat)
{
    uint packed = asuint(packedFloat);

    // Extract the bytes
    uint b1 = (packed >> 24) & 0xFF;
    uint b2 = (packed >> 16) & 0xFF;
    uint b3 = (packed >> 8) & 0xFF;
    uint b4 = packed & 0xFF;

    return int4(b1, b2, b3, b4);
}