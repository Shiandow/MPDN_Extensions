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
// -- Edge detection options -- 
#define acuity 25.0
#define radius 0.5
#define power 1.0

// -- Misc --
sampler s0 : register(s0);
sampler sUV : register(s1);

float4 p0	  : register(c0);
float2 p1	  : register(c1);
float4 size1  : register(c2);
float4 args0  : register(c3);

#define width  (p0[0])
#define height (p0[1])
#define chromaSize size1

#define dxdy (p1.xy)
#define ddxddy (chromaSize.zw)
#define chromaOffset (args0.xy)

// -- Window Size --
#define taps 3
#define even (taps - 2 * (taps / 2) == 0)
#define minX (1-ceil(taps/2.0))
#define maxX (floor(taps/2.0))

// -- Convenience --
#define sqr(x) dot(x,x)

// -- Input processing --
//Current high res value
#define GetY(x,y)      (tex2D(s0,ddxddy*(pos+chromaOffset+int2(x,y)+0.5))[0])
//Low res values
#define GetUV(x,y)    (tex2D(sUV,ddxddy*(pos+int2(x,y)+0.5)).yz)

// -- Colour space Processing --
#define Kb args0[2]
#define Kr args0[3]
#include "../Common/ColourProcessing.hlsl"

// -- Main Code --
float4 main(float2 tex : TEXCOORD0) : COLOR{
    float4 c0 = tex2D(s0, tex);
    float y = c0.x;

    // Calculate position
    float2 pos = tex * chromaSize.xy - chromaOffset - 0.5;
    float2 offset = pos - (even ? floor(pos) : round(pos));
    pos -= offset;

    // Calculate mean
    float weightSum = 0;
    float2 meanUV = 0;
   
    [unroll] for (int X = minX; X <= maxX; X++)
    [unroll] for (int Y = minX; Y <= maxX; Y++)
    {
        float dI2 = sqr(acuity*(y - GetY(X,Y)));
        float dXY2 = sqr((float2(X,Y) - offset)/radius);

        float weight = exp(-0.5*dXY2) * pow(1 + dI2/power, - power);
        //float weight = pow(rsqrt(dXY2 + dI2),3);
        
        meanUV += weight*GetUV(X,Y);
        weightSum += weight;
    }
    meanUV /= weightSum;

    // Update c0
    c0.yz = meanUV;
    
    return c0;
}