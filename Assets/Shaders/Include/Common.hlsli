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

struct VSInputVoxel
{
    float data : POSITION0;
};

struct PSInputVoxel
{
    float4 pos : SV_POSITION;
    float3 worldpos : POSITION;
    float3 camerapos : POSITION1;
    float3 lookat : POSITION2;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

float3 UnpackPosition(float packedFloat)
{
    uint packed = asuint(packedFloat);

    float X = packed & 0x3F; // 6 bits: 0-5
    float Y = (packed >> 6) & 0x3F; // 6 bits: 6-11
    float Z = (packed >> 12) & 0x3F; // 6 bits: 12-17

    return (float3(X, Y, Z) / 2) + 1;
}

int4 UnpackAttributes(float packedFloat)
{
    uint packed = asuint(packedFloat);

    uint vertexIndex = (packed >> 18) & 0x3; // 2 bits: 18-19
    uint normalIndex = (packed >> 20) & 0x7; // 3 bits: 20-22
    uint textureIndex = (packed >> 23) & 0xFF; // 8 bits: 23-30
    uint indent = (packed >> 31) & 0x1; // 1 bit: 31

    return int4(vertexIndex, normalIndex, textureIndex, indent);
}

float GetAtlasTileSize(int rowsColumns = 4)
{
    return 1.0f / rowsColumns;
}

float2 GetAtlasTileCoordinate(int index, int resolution = 2048, int rowsColumns = 4)
{
    float atlasTileSize = GetAtlasTileSize();

    return float2(atlasTileSize * (index % rowsColumns),
                  atlasTileSize * (index / rowsColumns));
}

float3 GetNormal(int index)
{
    float3 normal[6] =
    {
        float3(0, 1, 0),
        float3(0, -1, 0),
        float3(0, 0, 1),
        float3(0, 0, -1),
        float3(1, 0, 0),
        float3(-1, 0, 0),
    };

    return normal[index];
}

float3 GetTangent(int index)
{
    float3 tangent[6] =
    {
        float3(0, 0, 1),
        float3(0, 0, -1),
        float3(0, 1, 0),
        float3(0, -1, 0),
        float3(0, 1, 0),
        float3(0, -1, 0),
    };
    
    return tangent[index];
}

float2 GetUV(int index)
{
    float2 uv[4] =
    {
        float2(1, 1),
        float2(1, 0),
        float2(0, 0),
        float2(0, 1)
    };

    return uv[index];
}