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

texture velocityTexture;
sampler2D velocitySampler = sampler_state
{
    Texture = <velocityTexture>;
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

float2 texelSize;

float4 ComputeDivergencePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float obsC = tex2D(obstacleSampler, pos).r;
    if (obsC > 0.1f) {
        return float4(0, 0, 0, 1);
    }

    float obsL = tex2D(obstacleSampler, pos - float2(texelSize.x, 0)).r;
    float obsR = tex2D(obstacleSampler, pos + float2(texelSize.x, 0)).r;
    float obsT = tex2D(obstacleSampler, pos - float2(0, texelSize.y)).r;
    float obsB = tex2D(obstacleSampler, pos + float2(0, texelSize.y)).r;

    float2 vL = tex2D(velocitySampler, pos - float2(texelSize.x, 0)).xy;
    float2 vR = tex2D(velocitySampler, pos + float2(texelSize.x, 0)).xy;
    float2 vT = tex2D(velocitySampler, pos - float2(0, texelSize.y)).xy;
    float2 vB = tex2D(velocitySampler, pos + float2(0, texelSize.y)).xy;

    float halfRdx = 0.5f / texelSize.x;

    float divergence = halfRdx * ((vR.x - vL.x) + (vB.y - vT.y));

    return float4(divergence, 0, 0, 1);
}

technique ComputeDivergence
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ComputeDivergencePS();
    }
}