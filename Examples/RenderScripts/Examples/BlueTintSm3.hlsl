// This file is a part of MPDN Extensions.
// https://github.com/zachsaw/MPDN_Extensions
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.
// 
sampler s0 : register(s0);
float4 args0 : register(c0);

#define color_r (args0.r)
#define color_g (args0.g)
#define color_b (args0.b)

float4 main(float2 tex : TEXCOORD0) : COLOR
{
    return float4(saturate(tex2D(s0, tex).rgb + float3(color_r, color_g, color_b)), 1);
}
