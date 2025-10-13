using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;
using Peridot.UI.Builder;

public class RunnerUI
{
    private VerticalLayoutGroup _runnerList;
    private ScrollArea _scrollArea;
    private Vector2 _basePosition;
    private Vector2 _runnerBasePosition;

    private int _cardWidth = 760;
    private int _cardHeight = 170;
    
    private UIElement _root;

    public RunnerUI(RunnerManager runnerManager)
    {
        runnerManager.OnRunnersUpdated += UpdateRunners;

        CreateUI();
    }

    public UIElement CreateUI()
    {
        _basePosition = TabUIGlobals.WindowPosition;
        _runnerBasePosition = new Vector2(_basePosition.X + 15, _basePosition.Y + 40);
        var markup = $"""
        <canvas name="RunnerCanvas" bounds="{_basePosition.X},{_basePosition.Y},{TabUIGlobals.WindowSize.X},{TabUIGlobals.WindowSize.Y}" backgroundColor="#222222" clipToBounds="true">
            <div bounds="{_basePosition.X},{_basePosition.Y},{TabUIGlobals.WindowSize.X},{TabUIGlobals.WindowSize.Y}" spacing="10" orientation="vertical">
                <label name="HeaderLabel" bounds="{_basePosition.X},{_basePosition.Y},800,40" text="Runners" backgroundColor="#555555" textColor="#FFFFFF"/>
                <scrollarea name="RunnerScrollArea" bounds="{_basePosition.X},{_basePosition.Y + 100},{TabUIGlobals.WindowSize.X},{TabUIGlobals.WindowSize.Y - 50}">
                    <div name="RunnerList" bounds="{_basePosition.X},{_basePosition.Y + 100},{TabUIGlobals.WindowSize.X - 50},{TabUIGlobals.WindowSize.Y}" direction="vertical" spacing="10">
                    </div>
                </scrollarea>
            </div>
        </canvas>
        """;

        var builder = new UIBuilder(Core.DefaultFont);
        var mainContainer = (Canvas)builder.BuildFromMarkup(markup);
        //mainContainer.SetVisibility(false); // Start hidden, toggle visibility in code
        _runnerList = mainContainer.FindChildByName("RunnerList") as VerticalLayoutGroup;
        _scrollArea = mainContainer.FindChildByName("RunnerScrollArea") as ScrollArea;
        Core.UISystem.AddElement(mainContainer);
        _root = mainContainer;

        return mainContainer;
    }

    private UIElement CreateAgentCard(string agentName)
    {
        var markup = $"""
        <div name="AgentCard" bounds="0,100,{_cardWidth},{_cardHeight}" backgroundColor="#444444" direction="horizontal" spacing="10">
            <div direction="horizontal" bounds="0,30,{_cardWidth-100},{_cardHeight}">
                <div bounds="0,30,110,{_cardHeight}" backgroundColor="#666666">
                    <label name="AgentNameLabel" bounds="0,30,100,30" text="{agentName}" textColor="#FFFFFF"/>
                    <image bounds="0,100,100,100" source="images/ui/avatar"/>
                </div>
                <image bounds="0,0,100,100" source="images/ui/avatar"/>
                <image bounds="0,0,100,100" source="images/ui/avatar"/>
                <image bounds="0,0,100,100" source="images/ui/avatar"/>
            </div>
            <button name="SelectButton" bounds="0,0,80,30" text="Select" backgroundColor="#666666" textColor="#FFFFFF"/>
        </div>
        """;
        var builder = new UIBuilder(Core.DefaultFont);
        var agentCard = builder.BuildFromMarkup(markup);
        return agentCard;
    }

    private void UpdateRunners(List<RunnerData> runners)
    {
        _runnerList.ClearChildren();

        _runnerList.SetBounds(new Rectangle(_runnerBasePosition.ToPoint(), new Point((int)TabUIGlobals.WindowSize.X - 50, runners.Count * (_cardHeight + 10))));

        foreach (var runner in runners)
        {
            var agentCard = CreateAgentCard(runner.Name);
            _runnerList.AddChild(agentCard);
        }

        _scrollArea.RefreshContentBounds();
    }

    public void SetVisibility(bool isVisible)
    {
        _root.SetVisibility(isVisible);
    }
}