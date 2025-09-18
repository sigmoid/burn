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

texture sourceTexture;
sampler2D sourceSampler = sampler_state
{
    Texture = <sourceTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture obstacleTexture;
sampler2D obstacleSampler = sampler_state
{
    Texture = <obstacleTexture>;
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

float2 texelSize;

float4 JacobiPressurePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float4 center = tex2D(sourceSampler, pos);

    float obsCenter = tex2D(obstacleSampler, pos).r;
    if(obsCenter > 0.1f) {
        return float4(0, 0, 0, 1);
    }

    float obsL = tex2D(obstacleSampler, pos - float2(texelSize.x, 0)).r;
    float obsR = tex2D(obstacleSampler, pos + float2(texelSize.x, 0)).r;
    float obsT = tex2D(obstacleSampler, pos - float2(0, texelSize.y)).r;
    float obsB = tex2D(obstacleSampler, pos + float2(0, texelSize.y)).r;

    float left   = obsL > 0.1 ? 0.0 : tex2D(sourceSampler, pos - float2(texelSize.x, 0)).r;
    float right  = obsR > 0.1 ? 0.0 : tex2D(sourceSampler, pos + float2(texelSize.x, 0)).r;
    float top    = obsT > 0.1 ? 0.0 : tex2D(sourceSampler, pos - float2(0, texelSize.y)).r;
    float bottom = obsB > 0.1 ? 0.0 : tex2D(sourceSampler, pos + float2(0, texelSize.y)).r;

    float div = tex2D(divergenceSampler, pos).x;

    float alpha = -((texelSize.x )*(texelSize.x ));
    
    float result = (left + right + top + bottom + alpha * div) / 4.0f;
    
    return float4(result,0,0,1);
}

technique JacobiPressure
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL JacobiPressurePS();
    }
}
