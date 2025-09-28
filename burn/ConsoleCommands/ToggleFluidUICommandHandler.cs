using Peridot;

public class ToggleFluidUICommandHandler : ConsoleCommandHandler
{
    UIElement _fluidSimUI;

    public ToggleFluidUICommandHandler(UIElement fluidSimUI)
        : base()
    {
        CommandName = "toggle_fluid_ui";
        _fluidSimUI = fluidSimUI;
    }

    public override void Execute(string[] args)
    {
        _fluidSimUI.SetVisibility(!_fluidSimUI.IsVisible());
    }
}