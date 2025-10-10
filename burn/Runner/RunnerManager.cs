using System;
using System.Collections.Generic;

public class RunnerManager
{
    private List<RunnerData> _runners;

    public RunnerManager()
    {
        _runners = new List<RunnerData>();
    }

    public void AddRunner(RunnerData runner)
    {
        _runners.Add(runner);
        OnRunnersUpdated?.Invoke(_runners);
    }

    public Action<List<RunnerData>> OnRunnersUpdated;
}