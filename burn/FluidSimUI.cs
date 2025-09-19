using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using burn.FluidSimulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot.UI;

public class FluidSimUI
{
    Dictionary<string, float> _floatValues = new Dictionary<string, float>();


    private SpriteFont _font;
    private FluidSimulator _fluidSim;

    public FluidSimUI(SpriteFont font, FluidSimulator fluidSim)
    {
        _font = font;
        _fluidSim = fluidSim;

    }

    public IUIElement GetUIElement()
    {
        _floatValues = new Dictionary<string, float>
        {
            // Properties that are used in RestartFluidSimulation method with their default values
            ["diffuseIterations"] = 20,
            ["pressureIterations"] = 20,
            ["fuelBurnTemperature"] = 20.0f,
            ["fuelConsumptionRate"] = 32.0f,
            ["ignitionTemperature"] = 0.3f,
            ["buoyancyConstant"] = 80.0f,
            ["gravity"] = -9.81f,
            ["velocityDampingCoefficient"] = 0.75f,
            ["coolingRate"] = 62.5f  // 125.0f / 2.0f from the original code
        };

        VerticalLayoutGroup vlg = new VerticalLayoutGroup(new Rectangle(10, 10, 220, 1000), 10);

        // Add float fields with appropriate ranges for each parameter
        vlg.AddChild(FloatField("diffuseIterations", 1f, 100f, "diffuseIterations"));
        vlg.AddChild(FloatField("pressureIterations", 1f, 100f, "pressureIterations"));
        vlg.AddChild(FloatField("fuelBurnTemperature", 1f, 100f, "fuelBurnTemperature"));
        vlg.AddChild(FloatField("fuelConsumptionRate", 1f, 100f, "fuelConsumptionRate"));
        vlg.AddChild(FloatField("ignitionTemperature", 0.1f, 2f, "ignitionTemperature"));
        vlg.AddChild(FloatField("buoyancyConstant", 0f, 1000f, "buoyancyConstant"));
        vlg.AddChild(FloatField("gravity", -50f, 50f, "gravity"));
        vlg.AddChild(FloatField("velocityDampingCoefficient", 0f, 1f, "velocityDampingCoefficient"));
        vlg.AddChild(FloatField("coolingRate", 1f, 200f, "coolingRate"));

        // Add a button to apply changes to the fluid simulation
        var applyButton = new Button(
            new Rectangle(0, 0, 180, 40),
            "Apply Changes",
            _font,
            Color.Green,
            Color.LightGreen,
            Color.White,
            () => RestartFluidSimulation()
        );
        vlg.AddChild(applyButton);

        return vlg;
    }

    // Method to get current float values for use in simulation
    public float GetFloatValue(string propertyName)
    {
        return _floatValues.ContainsKey(propertyName) ? _floatValues[propertyName] : 0f;
    }

    // Method to get all current values
    public Dictionary<string, float> GetAllValues()
    {
        return new Dictionary<string, float>(_floatValues);
    }

    // Method to set a value programmatically
    public void SetFloatValue(string propertyName, float value)
    {
        if (_floatValues.ContainsKey(propertyName))
        {
            _floatValues[propertyName] = value;
        }
    }

    public void RestartFluidSimulation()
    { 
        _fluidSim.RestartFluidSimulation(_floatValues);
        Console.WriteLine("Fluid simulation parameters updated!");
    }

    private IUIElement FloatField(string label, float minValue, float maxValue, string propertyName)
    {
        HorizontalLayoutGroup hlg = new HorizontalLayoutGroup(new Rectangle(0, 0, 200, 30), 5);
        Peridot.UI.Label lbl = new Peridot.UI.Label(new Rectangle(10, 10, 100, 30), label, _font, Color.White);

        // Create text input with proper constructor parameters
        var textInput = new TextInput(
            new Rectangle(0, 0, 60, 30),
            _font,
            placeholder: "",
            backgroundColor: Color.White,
            textColor: Color.Black,
            borderColor: Color.DarkGray,
            focusedBorderColor: Color.LightGray
        );

        // Set the initial text value after construction
        textInput.Text = _floatValues[propertyName].ToString("F2"); // Format to 2 decimal places

        textInput.OnTextChanged += (text) =>
        {
            // Allow typing decimal numbers - only validate when complete
            if (string.IsNullOrEmpty(text))
            {
                return; // Allow empty text while typing
            }

            // Check if it's a valid partial number (like "1." or "0.5")
            if (IsValidPartialFloat(text))
            {
                // Try to parse complete numbers only if the text looks complete
                if (float.TryParse(text, out float value))
                {
                    // Only clamp if the value is way out of range (allow temporary out-of-range while typing)
                    if (value < minValue * 0.1f || value > maxValue * 10f)
                    {
                        // Revert to last valid value
                        textInput.Text = _floatValues[propertyName].ToString("F2");
                    }
                    else
                    {
                        // Update the stored value even if out of range (will be clamped on focus loss or enter)
                        _floatValues[propertyName] = Math.Max(minValue, Math.Min(maxValue, value));
                    }
                }
                // If it can't parse but is valid partial (like "1." or "-"), don't interfere
            }
            else
            {
                // Invalid input - revert to last valid value only if it's clearly invalid
                if (!text.EndsWith(".") && !text.EndsWith("-"))
                {
                    textInput.Text = _floatValues[propertyName].ToString("F2");
                }
            }
        };

        // Validate and format when focus is lost
        textInput.OnFocusLost += () =>
        {
            if (float.TryParse(textInput.Text, out float value))
            {
                // Clamp to proper range
                value = Math.Max(minValue, Math.Min(maxValue, value));
                _floatValues[propertyName] = value;
                textInput.Text = value.ToString("F2");
            }
            else
            {
                // Invalid text, revert to stored value
                textInput.Text = _floatValues[propertyName].ToString("F2");
            }
        };

        // Also validate on Enter key
        textInput.OnEnterPressed += (text) =>
        {
            if (float.TryParse(text, out float value))
            {
                value = Math.Max(minValue, Math.Min(maxValue, value));
                _floatValues[propertyName] = value;
                textInput.Text = value.ToString("F2");
            }
            else
            {
                textInput.Text = _floatValues[propertyName].ToString("F2");
            }
        };

        var multiplyButton = new Button(new Rectangle(0, 0, 30, 30), "*2", _font, Color.Gray, Color.LightGray, Color.White, () =>
        {
            _floatValues[propertyName] *= 2;
            if (_floatValues[propertyName] > maxValue)
                _floatValues[propertyName] = maxValue;
            textInput.Text = _floatValues[propertyName].ToString();
        });

        var divideButton = new Button(new Rectangle(0, 0, 30, 30), "/2", _font, Color.Gray, Color.LightGray, Color.White, () =>
        {
            _floatValues[propertyName] /= 2;
            if (_floatValues[propertyName] < minValue)
                _floatValues[propertyName] = minValue;
            textInput.Text = _floatValues[propertyName].ToString();
        });

        hlg.AddChild(lbl);
        hlg.AddChild(textInput);
        hlg.AddChild(divideButton);
        hlg.AddChild(multiplyButton);

        return hlg;
    }

    // Helper method to validate partial float input
    private bool IsValidPartialFloat(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;
            
        // Allow just negative sign at the beginning
        if (text == "-")
            return true;
            
        // Allow just decimal point
        if (text == ".")
            return true;
            
        // Allow negative decimal point
        if (text == "-.")
            return true;
            
        // Allow single decimal point anywhere in the number
        int decimalCount = text.Count(c => c == '.');
        if (decimalCount > 1)
            return false;
            
        // Check if all characters are valid
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            // First character can be negative sign
            if (i == 0 && c == '-')
                continue;
                
            // Any character can be a digit
            if (char.IsDigit(c))
                continue;
                
            // Any character can be a decimal point (but only one total)
            if (c == '.')
                continue;
                
            // Invalid character found
            return false;
        }
        
        return true;
    }
}