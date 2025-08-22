#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Parameters
float timeStep;
float diffusion;
float viscosity;
float2 texelSize;

// Textures
texture velocityTexture;
texture densityTexture;
texture pressureTexture;
texture sourceTexture;

sampler2D velocitySampler = sampler_state
{
    Texture = <velocityTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D densitySampler = sampler_state
{
    Texture = <densityTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D pressureSampler = sampler_state
{
    Texture = <pressureTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D sourceSampler = sampler_state
{
    Texture = <sourceTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Vertex shader input/output
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

// Vertex shader
VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord;
    return output;
}

// Helper functions
float2 sampleVelocity(float2 coord)
{
    return tex2D(velocitySampler, coord).xy;
}

float sampleDensity(float2 coord)
{
    return tex2D(densitySampler, coord).x;
}

float samplePressure(float2 coord)
{
    return tex2D(pressureSampler, coord).x;
}

// Advection - move quantities through the velocity field
float4 AdvectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = sampleVelocity(pos);
    
    // Trace particle backwards
    float2 prevPos = pos - velocity * texelSize * timeStep;
    
    // Sample the source texture at the previous position
    float4 result = tex2D(sourceSampler, prevPos);
    
    return result;
}

// Diffusion - spread quantities over time
float4 DiffusePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float4 center = tex2D(sourceSampler, pos);
    
    // Sample neighbors
    float4 left = tex2D(sourceSampler, pos - float2(texelSize.x, 0));
    float4 right = tex2D(sourceSampler, pos + float2(texelSize.x, 0));
    float4 top = tex2D(sourceSampler, pos - float2(0, texelSize.y));
    float4 bottom = tex2D(sourceSampler, pos + float2(0, texelSize.y));
    
    // Diffuse using Jacobi iteration
    float alpha = texelSize.x * texelSize.y / (diffusion * timeStep);
    float beta = 1.0f / (4.0f + alpha);
    
    float4 result = (left + right + top + bottom + alpha * center) * beta;
    
    return result;
}

// Compute pressure from velocity divergence
float4 ComputePressurePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    
    // Calculate divergence
    float2 vL = sampleVelocity(pos - float2(texelSize.x, 0));
    float2 vR = sampleVelocity(pos + float2(texelSize.x, 0));
    float2 vT = sampleVelocity(pos - float2(0, texelSize.y));
    float2 vB = sampleVelocity(pos + float2(0, texelSize.y));
    
    float divergence = 0.5f * ((vR.x - vL.x) + (vB.y - vT.y));
    
    // Return divergence as pressure
    return float4(divergence, 0, 0, 1);
}

// Project velocity to be mass-conserving (divergence-free)
float4 ProjectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = sampleVelocity(pos);
    
    // Sample pressure at neighboring cells
    float pL = samplePressure(pos - float2(texelSize.x, 0));
    float pR = samplePressure(pos + float2(texelSize.x, 0));
    float pT = samplePressure(pos - float2(0, texelSize.y));
    float pB = samplePressure(pos + float2(0, texelSize.y));
    
    // Calculate pressure gradient
    float2 gradient = float2(pR - pL, pB - pT) * 0.5f;
    
    // Subtract gradient from velocity
    velocity -= gradient;
    
    return float4(velocity, 0, 1);
}

// Visualize the fluid
float4 VisualizePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float density = sampleDensity(pos);
    float2 velocity = sampleVelocity(pos);
    
    // Create a color based on density and velocity
    float3 color = float3(density, density * 0.5f, density * 0.2f);
    
    // Add velocity visualization
    float speed = length(velocity) * 2.0f;
    color += float3(0, speed * 0.2f, speed * 0.5f);
    
    return float4(color, 1.0f);
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

technique ComputePressure
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ComputePressurePS();
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

technique Visualize
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL VisualizePS();
    }
}
