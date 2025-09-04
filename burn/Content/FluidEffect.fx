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
    float2 ndc = (input.Position.xy / renderTargetSize) * 2.0 - 1.0;
    
    output.Position = float4(ndc, 0, 1);
    output.TexCoord = float2(input.TexCoord.x, 1.0 - input.TexCoord.y);
    return output;
}

float timeStep;
float diffusion;
float2 texelSize;
float2 cursorPosition;
float2 cursorValue;
float radius;

texture fuelTexture;
sampler2D fuelSampler = sampler_state
{
    Texture = <fuelTexture>;
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

texture temperatureTexture;
sampler2D temperatureSampler = sampler_state
{
    Texture = <temperatureTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 DiffusePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float4 center = tex2D(sourceSampler, pos);
    
    if (diffusion <= 0.000001f)
    {
        return center;
    }

    float4 left = tex2D(sourceSampler, pos - float2(texelSize.x, 0));
    float4 right = tex2D(sourceSampler, pos + float2(texelSize.x, 0));
    float4 top = tex2D(sourceSampler, pos - float2(0, texelSize.y));
    float4 bottom = tex2D(sourceSampler, pos + float2(0, texelSize.y));

    float alpha = ((texelSize.x )*(texelSize.x )) / (diffusion * timeStep);
    float beta = 1.0f / (4.0f + alpha);
    
    float4 result = (left + right + top + bottom + alpha * center) * beta;
    
    return result;
}

float4 ComputeDivergencePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    
    float2 vL = tex2D(velocitySampler, pos - float2(texelSize.x, 0)).xy;
    float2 vR = tex2D(velocitySampler, pos + float2(texelSize.x, 0)).xy;
    float2 vT = tex2D(velocitySampler, pos - float2(0, texelSize.y)).xy;
    float2 vB = tex2D(velocitySampler, pos + float2(0, texelSize.y)).xy;
    
    float halfRdx = 0.5f / (texelSize.x );

    float divergence = halfRdx * ((vR.x - vL.x) + (vB.y - vT.y));
    
    return float4(divergence, 0, 0, 1);
}

float4 VisualizePS(VertexShaderOutput input) : COLOR0
{
    float2 visTexCoord = input.TexCoord;

    float fuel = tex2D(fuelSampler, visTexCoord).r;

    float velocityX = tex2D(velocitySampler, visTexCoord).x;
    float velocityY = tex2D(velocitySampler, visTexCoord).y;

    float pressure = tex2D(pressureSampler, visTexCoord).x;

    float divergence = tex2D(divergenceSampler, visTexCoord).x;

    float temperature = tex2D(temperatureSampler, visTexCoord).r;

    return float4(temperature, fuel, 0, 1);
}

float4 AdvectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = tex2D(velocitySampler, pos).xy;
    
    float2 prevPos = pos - velocity * texelSize * timeStep;
    
    float4 result = tex2D(sourceSampler, prevPos);
    
    return result;
}

float4 ProjectPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float2 velocity = tex2D(velocitySampler, pos).xy;
    
    float pL = tex2D(pressureSampler, pos - float2(texelSize.x, 0)).x;
    float pR = tex2D(pressureSampler, pos + float2(texelSize.x, 0)).x;
    float pT = tex2D(pressureSampler, pos - float2(0, texelSize.y)).x;
    float pB = tex2D(pressureSampler, pos + float2(0, texelSize.y)).x;
    
    float halfRdx = 0.5f / (texelSize.x );

    float2 gradient = float2(pR - pL, pB - pT) * halfRdx;
    
    float2 res = velocity - gradient;
    
    return float4(res, 0, 1);
}

float4 JacobiPressurePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float4 center = tex2D(sourceSampler, pos);
    
    float4 left = tex2D(sourceSampler, pos - float2(texelSize.x, 0));
    float4 right = tex2D(sourceSampler, pos + float2(texelSize.x, 0));
    float4 top = tex2D(sourceSampler, pos - float2(0, texelSize.y));
    float4 bottom = tex2D(sourceSampler, pos + float2(0, texelSize.y));

    float div = tex2D(divergenceSampler, pos).x;

    float alpha = -((texelSize.x )*(texelSize.x ));
    float beta = 1.0f / 4.0f;
    
    float result = (left + right + top + bottom + alpha * div) * beta;
    
    return float4(result,0,0,1);
}

float4 ClampPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    float4 value = tex2D(sourceSampler, pos);
    
    value = saturate(value);
    
    return value;
}

float4 AddValuePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float4 existingValue = tex2D(sourceSampler, pos);

    float dist = distance(pos, cursorPosition);

    if (dist > radius)
    {
        return existingValue;
    }

    float falloff = 1.0 - (dist / radius);

    float2 addedValue = cursorValue * falloff;

    float4 result = existingValue + float4(addedValue, 0, 0);

    return result;
}

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