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

texture mainTexture;
sampler2D mainSampler = sampler_state
{
    Texture = <mainTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = NONE;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

texture sdfTexture;
sampler2D sdfSampler = sampler_state
{
    Texture = <sdfTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float burnAmount;


float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(mainSampler, uv);
    float sdf    = tex2D(sdfSampler, uv).r;

    if(sdf < burnAmount)
    {
        discard;
    }

    return color;
}

technique BurnDecay
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}