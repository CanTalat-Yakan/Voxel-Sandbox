float3 GetSkyColor(float3 worldpos, float3 camerapos)
{
    float3 topColor = float3(0.44, 0.51, 0.58);
    float3 middleColor = float3(0.81, 0.89, 0.95);
    float3 bottomColor = float3(0.09, 0.09, 0.09);
    
    float3 viewDir = normalize(worldpos - camerapos);
    float dotUp = dot(viewDir, float3(0, 1, 0));
    float3 skyColor;

    if (dotUp > 0) // Close to top
        skyColor = lerp(middleColor, topColor, (dotUp) / 0.5);
    else if (dotUp < 0) // Close to bottom
        skyColor = lerp(middleColor, bottomColor, (-dotUp) / 0.9);
    else // Middle
        skyColor = middleColor;

    return skyColor;
}

float MapValue(float oldValue, float oldMin, float oldMax, float newMin, float newMax)
{
    float newValue = 0;
    
    // Avoid division by zero if oldMin equals oldMax
    if (oldMax - oldMin == 0.0)
        // When the old range is zero, return the midpoint of the new range
        newValue = (newMin + newMax) * 0.5;
    else
        newValue = newMin + (oldValue - oldMin) * (newMax - newMin) / (oldMax - oldMin);

    newValue = min(newMax, newValue);
    newValue = max(newMin, newValue);
    
    return newValue;
}

float ApplyCurve(float x, float curve)
{
    // Ensure that the input value x is clamped between 0 and 1
    x = saturate(x);

    // Apply the curve using a power function
    return pow(x, curve);
}