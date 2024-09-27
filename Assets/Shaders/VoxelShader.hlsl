#include "Include\Common.hlsli"

cbuffer Properties : register(b10)
{
};

Texture2D texture0 : register(t0);
sampler sampler0 : register(s3);

PSInputVoxel VS(VSInputVoxel input)
{
    PSInputVoxel output;

    output.camerapos = Camera;
    output.lookat = ViewDirection;
    
    float3 pos = UnpackFloatToVector3(input.data.x);
    
    output.pos = mul(float4(pos, 1), mul(World, ViewProjection));
    output.worldpos = mul(float4(pos, 1), World);

    int4 attributes = UnpackFloatToBytes(input.data.y);

    int textureUVIndex = attributes.x;
    int textureIndex = attributes.y;
    int normalIndex = attributes.z;
    int lightValue = attributes.w;
    
    output.normal = GetNormal(normalIndex);
    output.tangent = GetTangent(normalIndex);
    
    output.uv = GetUV(textureUVIndex) * GetAtlasTileSize() + GetAtlasTileCoordinate(textureIndex);

    return output;
}

float4 PS(PSInputVoxel input) : SV_TARGET
{
    // Sample the base color texture
    float4 baseColor = texture0.Sample(sampler0, input.uv);

    float3 finalColor = baseColor.rgb - max(0, dot(input.normal, float3(0.1, -0.5, 0.3)) * 0.25);
    finalColor -= max(0, dot(input.normal, float3(-0.1, -0.5, -0.3)) * 0.1);
    finalColor *= 0.88;
    
    // Return the final color with the original alpha
    return float4(finalColor, baseColor.a);
}