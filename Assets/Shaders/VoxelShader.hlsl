#include "Include\Common.hlsli"
#include "Include\Helper.hlsli"

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
    
    float3 pos = UnpackPosition(input.data);
    
    output.pos = mul(float4(pos, 1), mul(World, ViewProjection));
    output.worldpos = mul(float4(pos, 1), World);

    int4 attributes = UnpackAttributes(input.data);

    int vertexIndex = attributes.x;
    int normalIndex = attributes.y;
    int textureIndex = attributes.z;
    int indent = attributes.w;
    
    output.normal = GetNormal(normalIndex);
    output.tangent = GetTangent(normalIndex);
    
    output.uv = GetUV(vertexIndex) * GetAtlasTileSize() + GetAtlasTileCoordinate(textureIndex);
    
    return output;
}

float4 PS(PSInputVoxel input) : SV_TARGET
{
    float2 ddx_uv = ddx(input.uv) * GetAtlasTileSize();
    float2 ddy_uv = ddy(input.uv) * GetAtlasTileSize();
    float4 baseColor = texture0.SampleGrad(sampler0, input.uv, ddx_uv, ddy_uv);

    float3 finalColor = baseColor.rgb - max(0, dot(input.normal, float3(0.1, -0.5, 0.3)) * 0.25);
    finalColor -= max(0, dot(input.normal, float3(-0.1, -0.5, -0.3)) * 0.1);
    finalColor *= 0.88;
    
    float distance = length(input.worldpos - input.camerapos);
    float chunkMaxDistance = 750;
    float skyBlend = MapValue(distance, 0, 300, 0, 1);
    skyBlend = ApplyCurve(skyBlend, 2);
    
    finalColor *= 1 - skyBlend;
    finalColor += skyBlend * GetSkyColor(input.worldpos, input.camerapos);
    
    return float4(finalColor, baseColor.a);
}