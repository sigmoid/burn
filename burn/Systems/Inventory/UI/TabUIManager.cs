using System;
using Microsoft.Xna.Framework;
using Peridot;
using Peridot.UI;
using Peridot.UI.Builder;

public class TabUIManager
{
    private float _height = 50;

    private RunnerUI _runnerUI;
    private InventoryUI _inventoryUI;
    private CraftingUI _craftingUI;

    private Canvas _mainCanvas;

    public TabUIManager(RunnerUI runnerUI, InventoryUI inventoryUI, CraftingUI craftingUI)
    {
        _runnerUI = runnerUI;
        _inventoryUI = inventoryUI;
        _craftingUI = craftingUI;

        CreateUI(_runnerUI, _inventoryUI, _craftingUI);
    }

    private void CreateUI(RunnerUI runnerUI, InventoryUI inventoryUI, CraftingUI craftingUI)
    {
        _runnerUI = runnerUI;
        _inventoryUI = inventoryUI;
        _craftingUI = craftingUI;

        var markup = $"""
        <canvas name="MainCanvas" bounds="{TabUIGlobals.WindowPosition.X},{TabUIGlobals.WindowPosition.Y - _height},{TabUIGlobals.WindowSize.X},{_height}" backgroundColor="#333333">
            <div bounds="{TabUIGlobals.WindowPosition.X},{TabUIGlobals.WindowPosition.Y - _height},{TabUIGlobals.WindowSize.X},{_height}" direction="horizontal" spacing="10">
                <button name="InventoryTabButton" bounds="10,10,150,{_height}" text="Inventory" backgroundColor="#555555" textColor="#FFFFFF" onClick="ShowInventory"/>
                <button name="RunnerTabButton" bounds="170,10,150,{_height}" text="Runners" backgroundColor="#555555" textColor="#FFFFFF" onClick="ShowRunners"/>
                <button name="CraftingTabButton" bounds="330,10,150,{_height}" text="Crafting" backgroundColor="#555555" textColor="#FFFFFF" onClick="ShowCrafting"/>
            </div>
        </canvas>
        """;

        var builder = new UIBuilder(Core.DefaultFont);
        builder.RegisterEventHandler("ShowInventory", (asdf) => ShowInventory());
        builder.RegisterEventHandler("ShowRunners", (asdf) => ShowRunners());
        builder.RegisterEventHandler("ShowCrafting", (asdf) => ShowCrafting());
        _mainCanvas = (Canvas)builder.BuildFromMarkup(markup);
        Core.UISystem.AddElement(_mainCanvas);
        _mainCanvas.SetVisibility(false); // Start hidden, toggle visibility in code
    }

    public void SetVisibility(bool isVisible)
    {
        if (isVisible)
        {
            _mainCanvas.SetVisibility(true);
            ShowInventory();
        }
        else
        {
            _mainCanvas.SetVisibility(false);
            _inventoryUI.SetVisibility(false);
            _runnerUI.SetVisibility(false);
            _craftingUI.SetVisibility(false);
        }
    }

    private void ShowInventory()
    {
        _inventoryUI.SetVisibility(true);
        _runnerUI.SetVisibility(false);
        _craftingUI.SetVisibility(false);
    }

    private void ShowRunners()
    {
        _inventoryUI.SetVisibility(false);
        _runnerUI.SetVisibility(true);
        _craftingUI.SetVisibility(false);
    }

    private void ShowCrafting()
    {
        _inventoryUI.SetVisibility(false);
        _runnerUI.SetVisibility(false);
        _craftingUI.SetVisibility(true);
    }
}