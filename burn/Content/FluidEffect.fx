#ifndef RENDERTARGETSIZE_DECLARED
float2 renderTargetSize;
#define RENDERTARGETSIZE_DECLARED
#endif
#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    // Convert pixel position to NDC (Y up)
    float2 ndc = (input.Position.xy / renderTargetSize) * 2.0 - 1.0;
    
    output.Position = float4(ndc, 0, 1);
    output.TexCoord = float2(input.TexCoord.x, 1.0 - input.TexCoord.y); // Flip Y for correct orientation
    return output;
}

float timeStep;
float diffusion;
float2 texelSize;
float2 cursorPosition;   // normalized coords of the force center (0-1)
float2 cursorValue;      // force vector (e.g., [-1..1] range)
float radius;           // radius in normalized coords (e.g., 0.05)

// Density texture sampler
texture densityTexture;
sampler2D densitySampler = sampler_state
{
    Texture = <densityTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = NONE;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

texture velocityTexture;
sampler2D velocitySampler = sampler_state
{
    Texture = <velocityTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};


texture sourceTexture;
sampler2D sourceSampler = sampler_state
{
    Texture = <sourceTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture pressureTexture;
sampler2D pressureSampler = sampler_state
{
    Texture = <pressureTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture divergenceTexture;
sampler2D divergenceSampler = sampler_state
{
    Texture = <divergenceTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Diffusion - spread quantities over time
float4 DiffusePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float4 center = tex2D(sourceSampler, pos);
    
    if (diffusion <= 0.000001f)
    {
        return center; // No diffusion — preserve field
    }

    // Sample neighbors
    float4 left = tex2D(sourceSampler, pos - float2(texelSize.x, 0));
    float4 right = tex2D(sourceSampler, pos + float2(texelSize.x, 0));
    float4 top = tex2D(sourceSampler, pos - float2(0, texelSize.y));
    float4 bottom = tex2D(sourceSampler, pos + float2(0, texelSize.y));

    // Diffuse using Jacobi iteration
    float alpha = ((texelSize.x * texelSize.y)*(texelSize.x * texelSize.y)) / (diffusion * timeStep);
    float beta = 1.0f / (4.0f + alpha);
    
    float4 result = (left + right + top + bottom + alpha * center) * beta;
    
    return result;
}

// Compute pressure from velocity divergence
float4 ComputeDivergencePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    
    // Calculate divergence
    float2 vL = tex2D(velocitySampler, pos - float2(texelSize.x, 0)).xy;
    float2 vR = tex2D(velocitySampler, pos + float2(texelSize.x, 0)).xy;
    float2 vT = tex2D(velocitySampler, pos - float2(0, texelSize.y)).xy;
    float2 vB = tex2D(velocitySampler, pos + float2(0, texelSize.y)).xy;
    
    float halfRdx = 0.5f / texelSize.x;

    float divergence = halfRdx * ((vR.x - vL.x) + (vB.y - vT.y));
    
    // Return divergence as pressure
    return float4(divergence, 0, 0, 1);
}

float4 VisualizePS(VertexShaderOutput input) : COLOR0
{
    float2 visTexCoord = input.TexCoord;
    //visTexCoord.y = 1.0 - visTexCoord.y; // Flip Y for correct orientation

    float density = tex2D(densitySampler, visTexCoord).r;

    float velocityX = tex2D(velocitySampler, visTexCoord).x;
    float velocityY = tex2D(velocitySampler, visTexCoord).y;

    float pressure = tex2D(pressureSampler, visTexCoord).x;

    // Visualize as grayscale
    return float4(pressure, velocityX, velocityY, 1);
}

// Advection - move quantities through the velocity field
float4 AdvectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = tex2D(velocitySampler, pos).xy;
    
    // Trace particle backwards
    float2 prevPos = pos - velocity * texelSize * timeStep;
    
    // Sample the source texture at the previous position
    float4 result = tex2D(sourceSampler, prevPos);
    
    return result;
}

// Project velocity to be mass-conserving (divergence-free)
float4 ProjectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = tex2D(velocitySampler, pos).xy;
    
    // Sample pressure at neighboring cells
    float pL = tex2D(pressureSampler, pos - float2(texelSize.x, 0)).x;
    float pR = tex2D(pressureSampler, pos + float2(texelSize.x, 0)).x;
    float pT = tex2D(pressureSampler, pos - float2(0, texelSize.y)).x;
    float pB = tex2D(pressureSampler, pos + float2(0, texelSize.y)).x;
    
    float halfRdx = 0.5f / texelSize.x;

    // Calculate pressure gradient
    float2 gradient = float2(pR - pL, pB - pT) * halfRdx;
    
    // Subtract gradient from velocity
    velocity -= gradient;
    
    return float4(velocity, 0, 1);
}

float4 JacobiPressurePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float4 center = tex2D(sourceSampler, pos);
    
    // Sample neighbors
    float4 left = tex2D(sourceSampler, pos - float2(texelSize.x, 0));
    float4 right = tex2D(sourceSampler, pos + float2(texelSize.x, 0));
    float4 top = tex2D(sourceSampler, pos - float2(0, texelSize.y));
    float4 bottom = tex2D(sourceSampler, pos + float2(0, texelSize.y));

    float div = tex2D(divergenceSampler, pos).x;
    
    float result = (left + right + top + bottom - div) * 0.25f;
    
    return float4(result,0,0,1);
}

float4 ClampPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float4 value = tex2D(sourceSampler, pos);
    
    // Clamp values to [0, 1]
    value = saturate(value);
    
    return value;
}

float4 AddValuePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    // Sample existing velocity
    float4 existingValue = tex2D(sourceSampler, pos);

    // Calculate distance to the force center
    float dist = distance(pos, cursorPosition);

    // Outside radius, just return existing velocity
    if (dist > radius)
    {
        return existingValue;
    }

    // Smooth linear falloff
    float falloff = 1.0 - (dist / radius);

    // Apply force scaled by falloff
    float2 addedValue = cursorValue * falloff;

    // Add force to velocity channels (R, G)
    float4 result = existingValue + float4(addedValue, 0, 0);

    return result;
}

// Techniques
technique Advect
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL AdvectPS();
    }
}

technique Diffuse
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL DiffusePS();
    }
}

technique Visualize
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL VisualizePS();
    }
}

technique Project
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ProjectPS();
    }
}

technique ComputeDivergence
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ComputeDivergencePS();
    }
}

technique Clamp
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ClampPS();
    }
}

technique AddValue
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL AddValuePS();
    }
}

technique JacobiPressure
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL JacobiPressurePS();
    }
}