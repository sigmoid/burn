using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;

namespace burn;

/// <summary>
/// A simple test class to demonstrate UI input blocking functionality.
/// This creates a test UI with buttons and text inputs to verify that input is blocked
/// when interacting with UI elements.
/// </summary>
public class InputBlockingTest
{
    private Canvas _testCanvas;
    private Button _testButton;
    private TextInput _testTextInput;
    private TextArea _testTextArea;
    private Label _statusLabel;
    private bool _isVisible = false;

    public InputBlockingTest()
    {
        Initialize();
    }

    private void Initialize()
    {
        var font = Core.DefaultFont;
        
        // Create a test canvas to hold our UI elements
        _testCanvas = new Canvas(
            new Rectangle(50, 50, 450, 400),
            Color.Gray * 0.8f,
            true
        );
        _testCanvas.Name = "InputBlockingTestCanvas";

        // Create a test button
        _testButton = new Button(
            new Rectangle(20, 20, 150, 40),
            "Test Button",
            font,
            Color.DarkBlue,
            Color.Blue,
            Color.White,
            OnButtonClick
        );
        _testButton.Name = "TestButton";

        // Create a test text input
        _testTextInput = new TextInput(
            new Rectangle(20, 80, 200, 30),
            font,
            "Type here to test input blocking...",
            Color.White,
            Color.Black,
            Color.Gray,
            Color.CornflowerBlue
        );
        _testTextInput.Name = "TestTextInput";

        // Create a test text area
        _testTextArea = new TextArea(
            new Rectangle(20, 130, 200, 80),
            font,
            wordWrap: true,
            readOnly: false,
            backgroundColor: Color.White,
            textColor: Color.Black,
            borderColor: Color.Gray,
            focusedBorderColor: Color.CornflowerBlue
        );
        _testTextArea.Text = "Multi-line text area\nClick to focus and test\nkeyboard blocking";
        _testTextArea.Name = "TestTextArea";

        // Create a status label to show input blocking status
        _statusLabel = new Label(
            new Rectangle(20, 230, 400, 120),
            "Press 'T' to toggle this test UI\nObserve how inputs are blocked when UI is active",
            font,
            Color.Yellow
        );
        _statusLabel.Name = "StatusLabel";

        // Add elements to the canvas
        _testCanvas.AddChild(_testButton);
        _testCanvas.AddChild(_testTextInput);
        _testCanvas.AddChild(_testTextArea);
        _testCanvas.AddChild(_statusLabel);
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        
        if (_isVisible)
        {
            Core.UISystem.AddElement(_testCanvas);
        }
        else
        {
            Core.UISystem.RemoveElement(_testCanvas);
        }
    }

    public bool IsVisible => _isVisible;

    private void OnButtonClick()
    {
        // Update the status when button is clicked
        _statusLabel.SetText($"Button clicked at {System.DateTime.Now:HH:mm:ss}!\nInputs should be blocked from game when UI is active.");
    }

    public void Update()
    {
        // Check if 'T' key is pressed to toggle the test UI
        if (Core.InputManager.GetButton("TestUI")?.IsPressed == true)
        {
            ToggleVisibility();
        }

        // Update status based on UI input blocking state
        if (_isVisible)
        {
            bool mouseBlocked = Core.UISystem.ShouldBlockMouseInput();
            bool keyboardBlocked = Core.UISystem.ShouldBlockKeyboardInput();
            bool mouseOverUI = Core.UISystem.IsMouseOverUI();

            bool textInputFocused = _testTextInput.IsFocused;
            bool textAreaFocused = _testTextArea.IsFocused;
            
            string status = $"UI Test Active\n" +
                          $"Mouse Over UI: {mouseOverUI}\n" +
                          $"Mouse Blocked: {mouseBlocked}\n" +
                          $"Keyboard Blocked: {keyboardBlocked}\n" +
                          $"TextInput Focused: {textInputFocused}\n" +
                          $"TextArea Focused: {textAreaFocused}\n" +
                          $"Press 'T' to hide this UI";

            _statusLabel.SetText(status);
        }
    }
}