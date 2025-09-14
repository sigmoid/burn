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
float ignitionTemperature;
float fuelBurnTemperature;
float fuelConsumptionRate;
float vorticityScale;
float combustionPressure;
float ambientTemperature;
float maxTemperature;
float coolingRate;
float minFuelThreshold;
// Boundary condition parameters
float boundaryScale;
float2 boundaryOffset;

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

texture vorticityTexture;
sampler2D vorticitySampler = sampler_state
{
    Texture = <vorticityTexture>;
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

float4 VisualizePS(VertexShaderOutput input) : COLOR0
{
    float2 visTexCoord = input.TexCoord;

    float fuel = tex2D(fuelSampler, visTexCoord).r;

    float velocityX = tex2D(velocitySampler, visTexCoord).x;
    float velocityY = tex2D(velocitySampler, visTexCoord).y;

    float pressure = tex2D(pressureSampler, visTexCoord).x;

    float divergence = tex2D(divergenceSampler, visTexCoord).x;

    float temperature = tex2D(temperatureSampler, visTexCoord).r;

    float vorticity = tex2D(vorticitySampler, visTexCoord).x;

    float obstacle =  tex2D(obstacleSampler, visTexCoord).r;

    if(obstacle > 0.1f)
    {
        return float4(1,1,1,1);
    }   

    float blue = 0;

    if(temperature > 0.5)
    {
        blue = 1.0f;
    }


    return float4(temperature, fuel, blue, 1);
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

    float obstacle = tex2D(obstacleSampler, pos).r;

    if(obstacle > 0.1f)
    {
        return float4(0,0,0,1);
    }

    float2 velocity = tex2D(velocitySampler, pos).xy;
    
    float pL = tex2D(pressureSampler, pos - float2(texelSize.x, 0)).x;
    float pR = tex2D(pressureSampler, pos + float2(texelSize.x, 0)).x;
    float pT = tex2D(pressureSampler, pos - float2(0, texelSize.y)).x;
    float pB = tex2D(pressureSampler, pos + float2(0, texelSize.y)).x;
    
    float halfRdx = 0.5f / (texelSize.x );

    float2 gradient = float2(pR - pL, pB - pT) * halfRdx;
    
    float2 res = velocity - gradient;

    float obsL = tex2D(obstacleSampler, pos - float2(texelSize.x, 0)).r;
    float obsR = tex2D(obstacleSampler, pos + float2(texelSize.x, 0)).r;
    float obsT = tex2D(obstacleSampler, pos - float2(0, texelSize.y)).r;
    float obsB = tex2D(obstacleSampler, pos + float2(0, texelSize.y)).r;

    if (obsL > 0.1f && res.x < 0) res.x = 0;
    if (obsR > 0.1f && res.x > 0) res.x = 0;
    if (obsT > 0.1f && res.y < 0) res.y = 0;
    if (obsB > 0.1f && res.y > 0) res.y = 0;
    
    return float4(res, 0, 1);
}

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

float4 SetValuePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float dist = distance(pos, cursorPosition);

    if (dist <= radius)
    {
        return float4(cursorValue, 0, 0);
    }
    else
    {
        return tex2D(sourceSampler, pos);
    }
}

float4 ComputeVorticityPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float2 vL = tex2D(velocitySampler, pos - float2(texelSize.x, 0)).xy;
    float2 vR = tex2D(velocitySampler, pos + float2(texelSize.x, 0)).xy;
    float2 vT = tex2D(velocitySampler, pos - float2(0, texelSize.y)).xy;
    float2 vB = tex2D(velocitySampler, pos + float2(0, texelSize.y)).xy;

    float curl = (vR.y - vL.y) * 0.5f / texelSize.x - (vT.x - vB.x) * 0.5f / texelSize.y;

    return float4(abs(curl), curl, 0, 1);
}


float4 VorticityConfinementPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float left = tex2D(vorticitySampler, pos - float2(texelSize.x, 0)).x;
    float right = tex2D(vorticitySampler, pos + float2(texelSize.x, 0)).x;
    float top = tex2D(vorticitySampler, pos - float2(0, texelSize.y)).x;
    float bottom = tex2D(vorticitySampler, pos + float2(0, texelSize.y)).x;

    float2 gradient = float2(right - left, bottom - top);
    gradient = normalize(gradient + 1e-5); 

    float curl = tex2D(vorticitySampler, pos).y;

    float2 force = vorticityScale * float2(gradient.y, -gradient.x) * curl;

    float2 velocity = tex2D(velocitySampler, pos).xy;
    velocity += force * timeStep;

    return float4(velocity, 0, 1);
}

float4 IgnitionPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float fuel = tex2D(fuelSampler, pos).r;
    float temperature = tex2D(temperatureSampler, pos).r;

    float newTemperature = temperature;

    if (fuel > minFuelThreshold && temperature >= ignitionTemperature)
    {
        newTemperature += fuelBurnTemperature * timeStep;
    }

    return float4(newTemperature, 0, 0, 1);
}

float4 CombustionDivergencePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float fuel = tex2D(fuelSampler, pos).r;
    float temperature = tex2D(temperatureSampler, pos).r;
    float divergence = tex2D(sourceSampler, pos).x;

    float newDivergence = divergence;

    if (fuel > minFuelThreshold && temperature >= ignitionTemperature)
    {
        newDivergence += combustionPressure;
    }

    return float4(newDivergence, 0, 0, 1);
}

float4 ConsumeFuelPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float fuel = tex2D(fuelSampler, pos).r;
    float temperature = tex2D(temperatureSampler, pos).r;

    if (temperature > ignitionTemperature && fuel > 0.0f)
    {
        fuel -= fuelConsumptionRate * temperature * timeStep;
        fuel = max(fuel, 0.0f);
    }

    return float4(fuel, 0, 0, 1);
}

float4 RadiancePS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;

    float temperature = tex2D(temperatureSampler, pos).r;

    float newTemperature;

    float a = 1/ pow(temperature - ambientTemperature, 3);
    float b = (3 * coolingRate * timeStep) / pow (maxTemperature - ambientTemperature, 4);

    newTemperature = ambientTemperature + pow(a + b, -1.0/3.0);

    if(newTemperature < ambientTemperature)
    {
        newTemperature = ambientTemperature;
    }   

    return float4(newTemperature, 0, 0, 1);
}   

// Boundary condition pixel shader as described in GPU Gems Chapter 38
float4 BoundaryPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    
    // Sample from the interior cell (offset by boundaryOffset)
    float2 interiorPos = pos + boundaryOffset * texelSize;
    float4 interiorValue = tex2D(sourceSampler, interiorPos);
    
    // Apply boundary scale (for velocity: -1 for no-slip, for pressure: 1 for Neumann)
    return interiorValue * boundaryScale;
}

// Copy pixel shader for preserving interior values
float4 CopyPS(VertexShaderOutput input) : COLOR0
{
    float2 pos = input.TexCoord;
    return tex2D(sourceSampler, pos);
}

float4 SpreadFirePS(VertexShaderOutput input) : COLOR0
{
    float4 centerTemp = tex2D(temperatureSampler, input.TexCoord);
    float4 leftTemp = tex2D(temperatureSampler, input.TexCoord - float2(texelSize.x, 0));
    float4 rightTemp = tex2D(temperatureSampler, input.TexCoord + float2(texelSize.x, 0));
    float4 topTemp = tex2D(temperatureSampler, input.TexCoord - float2(0, texelSize.y));
    float4 bottomTemp = tex2D(temperatureSampler, input.TexCoord + float2(0, texelSize.y));

    float4 centerFuel = tex2D(fuelSampler, input.TexCoord);
    float4 leftFuel = tex2D(fuelSampler, input.TexCoord - float2(texelSize.x, 0));
    float4 rightFuel = tex2D(fuelSampler, input.TexCoord + float2(texelSize.x, 0));
    float4 topFuel = tex2D(fuelSampler, input.TexCoord - float2(0, texelSize.y));
    float4 bottomFuel = tex2D(fuelSampler, input.TexCoord + float2(0, texelSize.y));

    if (leftFuel.r > minFuelThreshold && leftTemp.r >= ignitionTemperature && centerTemp.r < ignitionTemperature && centerFuel.r > minFuelThreshold) centerTemp.r = ignitionTemperature;
    if (rightFuel.r > minFuelThreshold && rightTemp.r >= ignitionTemperature && centerTemp.r < ignitionTemperature && centerFuel.r > minFuelThreshold) centerTemp.r = ignitionTemperature;
    if (topFuel.r > minFuelThreshold && topTemp.r >= ignitionTemperature && centerTemp.r < ignitionTemperature && centerFuel.r > minFuelThreshold) centerTemp.r = ignitionTemperature;
    if (bottomFuel.r > minFuelThreshold && bottomTemp.r >= ignitionTemperature && centerTemp.r < ignitionTemperature && centerFuel.r > minFuelThreshold) centerTemp.r = ignitionTemperature;

    return centerTemp;
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

technique SetValue
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL SetValuePS();
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

technique ComputeVorticity
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ComputeVorticityPS();
    }
}

technique VorticityConfinement
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL VorticityConfinementPS();
    }
}

technique Ignition
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL IgnitionPS();
    }
}

technique ConsumeFuel
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ConsumeFuelPS();
    }
}

technique Radiance
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL RadiancePS();
    }
}

technique CombustionDivergence
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL CombustionDivergencePS();
    }
}

technique Boundary
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BoundaryPS();
    }
}

technique Copy
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL CopyPS();
    }
}

technique SpreadFire
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL SpreadFirePS();
    }
}