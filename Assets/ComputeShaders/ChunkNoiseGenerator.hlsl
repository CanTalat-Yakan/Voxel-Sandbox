#pragma kernel CS

struct SomeData
{
    float somehing;
};

RWStructuredBuffer<SomeData> Result;

[numthreads(8, 8, 1)]
void CS(uint3 id : SV_DispatchThreadID)
{
    // Calculate a linear index based on thread IDs
    uint index = id.x + id.y * 8;

    // Create an instance of SomeData and assign a value
    SomeData data;
    data.somehing = 1.0f; // Assign your desired value here

    // Write the data to the buffer at the calculated index
    Result[index] = data;
}
