#include "Include\Common.hlsli"

cbuffer Properties : register(b10)
{
    // Color
    float4 Color;
    // Header("This is a Header!")
    float Float;
    int Int;
    // Slider(1, 10)
    float Slider;
    // Space
    float2 Float2;
    float3 Float3;
    float4 Float4;
    // Space
    bool Bool;
};

Texture2D texture0 : register(t0);
sampler sampler0 : register(s3);

PSInputMin VS(VSInputMin input)
{
    PSInputMin output;

    output.pos = mul(float4(input.pos, 1), mul(World, ViewProjection));
    //output.normal = mul(float4(input.normal, 0), World);
    //output.tangent = mul(float4(input.tangent, 0), World);
    output.worldpos = mul(float4(input.pos, 1), World);
    output.camerapos = Camera;
    output.lookat = ViewDirection;
    output.uv = input.uv;

    return output;
}

float4 PS(PSInputMin input) : SV_TARGET
{
    // Sample the base color texture
    float4 baseColor = texture0.Sample(sampler0, input.uv);

    // Normalize the normal vector
    float3 normal = float3(0,1,0); // normalize(input.normal);

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
//float4 PS(PSInput input) : SV_TARGET
//{
//    // Sample the base color texture
//    float4 baseColor = texture0.Sample(sampler0, input.uv);

//    // Normalize the interpolated normal
//    float3 normal = normalize(input.normal);

//    // Calculate view direction
//    float3 viewDir = normalize(Camera - input.worldpos);

//    // Define light properties
//    float3 lightDirection = normalize(float3(0.5f, -1.0f, -0.5f)); // Directional light coming from above
//    float3 lightColor = float3(1.0f, 1.0f, 1.0f);
//    float ambientStrength = 0.2f;
//    float3 ambientColor = float3(1.0f, 1.0f, 1.0f);

//    // Ambient lighting
//    float3 ambient = ambientStrength * ambientColor;

//    // Diffuse lighting
//    float diffuseIntensity = saturate(dot(normal, -lightDirection));
//    float3 diffuse = diffuseIntensity * lightColor;

//    // Specular lighting
//    float3 reflectDir = reflect(lightDirection, normal);
//    float specularStrength = 0.5f;
//    float shininess = 32.0f;
//    float spec = pow(saturate(dot(viewDir, reflectDir)), shininess);
//    float3 specular = specularStrength * spec * lightColor;

//    // Rim lighting
//    float rimFactor = 1.0f - saturate(dot(normal, viewDir));
//    rimFactor = pow(rimFactor, 4.0f);
//    float3 rimColor = float3(0.5f, 0.5f, 1.0f) * rimFactor;

//    // Combine all lighting components
//    float3 finalColor = (ambient + diffuse + specular) * baseColor.rgb + rimColor;

//    return float4(finalColor, baseColor.a);
//}