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
    float2 data : TEXCOORD0;
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

float3 UnpackFloatToVector3(float packedFloat)
{
    uint packed = asuint(packedFloat);

    // Unpack a float into a float3 (X, Y, Z) where X and Z get 8 bits, Y gets 16 bits
    float X = (packed >> 24) & 0xFF; // Extract the 8 bits for X
    float Y = (packed >> 8) & 0xFFFF; // Extract the 16 bits for Y
    float Z = packed & 0xFF; // Extract the 8 bits for Z

    return float3(X, Y, Z);
}

int4 UnpackFloatToBytes(float packedFloat)
{
    uint packed = asuint(packedFloat);

    // Unpack a float into 4 bytes
    uint b1 = (packed >> 24) & 0xFF;
    uint b2 = (packed >> 16) & 0xFF;
    uint b3 = (packed >> 8) & 0xFF;
    uint b4 = packed & 0xFF;

    return int4(b1, b2, b3, b4);
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