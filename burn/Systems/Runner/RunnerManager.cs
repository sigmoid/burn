using System;
using System.Collections.Generic;
using burn.Systems.Inventory;
using Microsoft.Xna.Framework;

public class RunnerManager
{
    private List<RunnerData> _runners;
    private Dictionary<Guid, float> _currentRunTimers;
    private ItemDropManager _itemDropManager;
    private PlayerInventory _playerInventory;

    private float _runCooldown = 10f; // Example cooldown duration in seconds
    public RunnerManager(PlayerInventory inventory, ItemDropManager itemDropManager)
    {
        _runners = new List<RunnerData>();
        _currentRunTimers = new Dictionary<Guid, float>();
        _itemDropManager = itemDropManager;
        _playerInventory = inventory;
    }

    public void AddRunner(RunnerData runner)
    {
        _runners.Add(runner);
        OnRunnersUpdated?.Invoke(_runners);
    }

    public Action<List<RunnerData>> OnRunnersUpdated;

    public void StartRun(Guid runnerId)
    {
        var runner = _runners.Find(r => r.Id == runnerId);
        if (runner != null)
        {
            _currentRunTimers[runnerId] = _runCooldown; // Initialize timer
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var kvp in _currentRunTimers)
        {
            _currentRunTimers[kvp.Key] -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_currentRunTimers[kvp.Key] <= 0)
            {
                _currentRunTimers.Remove(kvp.Key);
                EndRun(kvp.Key);
                break;
            }
        }
    }

    private void EndRun(Guid runnerId)
    {
        var runner = _runners.Find(r => r.Id == runnerId);

        NotificationManager.ShowNotification($"Runner {runner.Name} has returned from the run!");
        var drop = _itemDropManager.GenerateMultipleDrops();

        foreach (var item in drop)
        {
            _playerInventory.AddItemWithNotification(item.Key, item.Value); 
        }
    }
    

    public bool CanStartRun(Guid runnerId)
    {
        var runner = _runners.Find(r => r.Id == runnerId);
        return runner != null; 
    }
}