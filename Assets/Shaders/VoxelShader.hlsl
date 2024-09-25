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

    // Normalize the normal vector
    float3 normal = normalize(input.normal);

    // Define light direction (from above)
    float3 lightDirection = normalize(float3(0.0f, -1.0f, 0.0f));

    // Calculate the dot product between normal and light direction
    float NdotL = saturate(dot(normal, -lightDirection));
        
    // Toon shading: quantize the lighting to create discrete steps
    const int toonLevels = 3; // Number of shading levels
    float quantized = floor(NdotL * toonLevels) / (toonLevels - 1);

    // Ensure quantized value is within [0, 1]
    quantized = saturate(quantized);

    // Apply the quantized lighting to the base color
    float3 finalColor = baseColor.rgb * quantized;

    // Increase saturation by blending with the base color
    float saturationAmount = 1.5f; // Adjust for more or less saturation
    finalColor = lerp(finalColor, baseColor.rgb, (saturationAmount - 1.0f) / saturationAmount);

    // Return the final color with the original alpha
    return float4(finalColor, baseColor.a);
}