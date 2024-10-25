﻿// Each #kernel tells which function to compile; you can have many kernels
#pragma kernel CS

// Create a RenderTexture with enableRandomWrite flag and set it
// with cs.SetTexture
RWTexture2D<float4> Result;
//RWStructuredBuffer 

[numthreads(8, 8, 1)]
void CS(uint3 id : SV_DispatchThreadID)
{
	// TODO: insert actual code here!

    Result[id.xy] = float4(id.x & id.y, (id.x & 15) / 15.0, (id.y & 15) / 15.0, 0.0);
}
