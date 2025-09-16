# Sprite-Based Obstacle System with Fuel Conversion

This document describes the sprite-based obstacle rendering system added to the fluid simulation, including the new fuel conversion feature.

## Overview

The sprite-based obstacle system allows you to render complex obstacle shapes using sprite textures instead of simple circles. Additionally, obstacle pixels can be automatically converted to fuel, creating dynamic fuel sources from sprite shapes. This provides much more flexibility for creating realistic environments in your fluid simulation.

## Components

### 1. New Render Targets
- `_spriteObstacleRT` - Primary render target for sprite-based obstacles
- `_tempSpriteObstacleRT` - Temporary render target for double buffering

### 2. DrawSpritesToObstacleStep Class
A simulation step that uses SpriteBatch to render sprites to the obstacle render target, with optional fuel conversion.

**Key Features:**
- Supports multiple sprites per step
- Configurable obstacle strength per sprite (0.0 - 1.0)
- Full sprite transformation support (position, rotation, scale)
- Customizable blend states
- Option to clear or preserve existing obstacles
- **NEW: Automatic obstacle-to-fuel conversion**
- **NEW: Configurable fuel conversion rate**

### 3. ObstacleSpriteData Structure
Data structure for defining sprite obstacles:
```csharp
public struct ObstacleSpriteData
{
    public Sprite Sprite;           // The sprite to render
    public Vector2 Position;        // World position
    public float Rotation;          // Rotation in radians
    public Vector2 Scale;           // Scale factor
    public Color Color;             // Color tint
    public float ObstacleValue;     // Obstacle strength (0-1)
}
```

### 4. Fuel Conversion Shader
**NEW: ObstacleToFuel Technique**
- Automatically converts obstacle pixels to fuel after sprite rendering
- Proportional fuel addition based on obstacle strength
- Configurable conversion rate per step
- Uses existing fuel accumulation system

## Usage

### Basic Setup
```csharp
// Create a sprite obstacle step with fuel conversion
var spriteObstacleStep = fluidSimulator.CreateSpriteObstacleStep(0, true, 1.0f);

// Create a sprite from a texture
var obstacleTexture = content.Load<Texture2D>("obstacles/rock");
var obstacleRegion = new TextureRegion(obstacleTexture, 0, 0, obstacleTexture.Width, obstacleTexture.Height);
var obstacleSprite = new Sprite(obstacleRegion);
obstacleSprite.CenterOrigin();

// Add the sprite as an obstacle that will be converted to fuel
spriteObstacleStep.AddSprite(
    obstacleSprite, 
    new Vector2(256, 256),  // Position
    1.0f,                   // Full obstacle strength
    0f,                     // No rotation
    Vector2.One             // Normal scale
);
```

### Fuel Conversion Control
```csharp
// Create step with fuel conversion enabled (default)
var fuelStep = fluidSimulator.CreateSpriteObstacleStep(0, true, 2.0f); // High conversion rate

// Create step without fuel conversion (obstacles only)
var obstacleOnlyStep = fluidSimulator.CreateSpriteObstacleStep(0, false);

// Create step with custom conversion rate
var customStep = fluidSimulator.CreateSpriteObstacleStep(0, true, 0.5f); // Slow conversion
```

### Advanced Usage

#### Multiple Obstacles
```csharp
// Add multiple obstacles with different properties
spriteObstacleStep.AddSprite(obstacleSprite, new Vector2(100, 100), 0.8f, MathHelper.ToRadians(45), Vector2.One);
spriteObstacleStep.AddSprite(obstacleSprite, new Vector2(400, 300), 0.6f, MathHelper.ToRadians(-30), Vector2.One * 1.5f);
```

#### Dynamic/Animated Obstacles
```csharp
// Update obstacles each frame for animation
spriteObstacleStep.ClearSprites();
float time = (float)gameTime.TotalGameTime.TotalSeconds;
Vector2 animatedPosition = new Vector2(
    centerX + (float)Math.Cos(time) * radius,
    centerY + (float)Math.Sin(time) * radius
);
spriteObstacleStep.AddSprite(obstacleSprite, animatedPosition, 0.9f, time, Vector2.One);
```

#### Custom Blend States
```csharp
// Create additive blending for accumulative obstacles
var additiveBlend = new BlendState
{
    ColorSourceBlend = Blend.SourceAlpha,
    ColorDestinationBlend = Blend.One,
    ColorBlendFunction = BlendFunction.Add
};

var step = new DrawSpritesToObstacleStep("spriteObstacle", false, additiveBlend);
```

## Integration with Fluid Simulation

The sprite obstacle system integrates seamlessly with the existing fluid simulation:

1. **Render Target Registration**: The new render targets are automatically registered with the RenderTargetProvider
2. **Simulation Pipeline**: Add the DrawSpritesToObstacleStep to your simulation steps
3. **Obstacle Interaction**: The rendered sprites act as obstacles that affect fluid flow
4. **Fuel Conversion**: Obstacle pixels are automatically converted to fuel using the ObstacleToFuel shader technique
5. **Visualization**: The sprite obstacles can be visualized alongside other simulation data

## Fuel Conversion System

The new fuel conversion feature adds a shader-based post-processing step that:

- **Reads Obstacle Data**: Samples from the sprite obstacle render target
- **Converts to Fuel**: Adds fuel proportional to obstacle strength and conversion rate
- **Preserves Existing Fuel**: Additively combines with existing fuel values
- **Real-time Processing**: Happens automatically after sprite rendering each frame

### Shader Details
The `ObstacleToFuel` technique:
- Uses the obstacle texture as input for conversion strength
- Multiplies obstacle values by `sourceStrength` and `timeStep` for frame-rate independence
- Writes results to the fuel render target using standard pipeline

## Performance Considerations

- **Sprite Count**: Each sprite requires a draw call, so batch similar sprites together
- **Texture Size**: Use appropriately sized textures for your simulation resolution
- **Update Frequency**: Only clear and re-add sprites when necessary for dynamic obstacles
- **Blend States**: Choose appropriate blend states based on your obstacle accumulation needs

## Example Integration

See `Examples/SpriteObstacleExample.cs` for complete working examples of:
- Basic sprite obstacle setup
- Animated/dynamic obstacles
- Custom blend state creation
- Multiple obstacle management

## API Reference

### FluidSimulator Methods
- `CreateSpriteObstacleStep(int insertIndex = 0, bool convertToFuel = true, float fuelConversionRate = 1.0f)` - Creates and adds a sprite obstacle step with fuel conversion options
- `RemoveSpriteObstacleSteps()` - Removes all sprite obstacle steps
- `GetSpriteObstacleRenderTarget()` - Gets the current sprite obstacle render target
- `SpriteBatch` property - Access to the internal SpriteBatch for custom rendering

### DrawSpritesToObstacleStep Methods
- `AddSprite(ObstacleSpriteData)` - Adds a sprite with full configuration
- `AddSprite(Sprite, Vector2, float, float, Vector2?)` - Adds a sprite with simplified parameters
- `ClearSprites()` - Removes all sprites from the step
- `SpriteCount` property - Gets the number of sprites to be rendered

### Constructor Parameters
- `targetField` - Target render target field name (default: "spriteObstacle")
- `clearTarget` - Whether to clear the target before drawing (default: true)
- `blendState` - Blend state for sprite rendering (default: AlphaBlend)
- `convertToFuel` - Whether to convert obstacle pixels to fuel after drawing (default: true)
- `fuelConversionRate` - Rate at which obstacles are converted to fuel (default: 1.0)
