using Peridot;

public class AddRunnerCommandHandler : ConsoleCommandHandler
{
    RunnerManager _runnerManager;
    public AddRunnerCommandHandler(RunnerManager runnerManager)
    {
        CommandName = "addrunner";
        _runnerManager = runnerManager;
    }

    public override void Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Logger.Error("Usage: addrunner <runner_name>");
            return;
        }

        var runnerName = args[0];
        var newRunner = new RunnerData { Name = runnerName };
        _runnerManager.AddRunner(newRunner);
        Logger.Info($"Added new runner: {runnerName}");
    }
}