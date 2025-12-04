using DatabaseSimulation.Models;
using DatabaseSimulation.Services;
using DiningPhilosophers.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DatabaseSimulation.HostedServices;

public abstract class PhilosopherHostedService : BackgroundService {
    protected readonly int Id;
    protected readonly string Name;
    protected readonly Fork LeftFork;
    protected readonly Fork RightFork;
    protected readonly IStrategy Strategy;
    protected readonly IMetricsCollector MetricsCollector;
    protected readonly IStateLogger StateLogger;
    protected readonly SimulationOptions Options;
    protected readonly Random Random;

    protected PhilosopherState State;
    protected bool HasLeftFork;
    protected bool HasRightFork;
    protected DateTime HungryStartTime;

    protected PhilosopherHostedService(
        int id,
        string name,
        ITableManager tableManager,
        IMetricsCollector metricsCollector,
        IStateLogger stateLogger,
        IStrategy strategy,
        IOptions<SimulationOptions> options) {
        Id = id;
        Name = name;
        MetricsCollector = metricsCollector;
        StateLogger = stateLogger;
        Strategy = strategy;
        Options = options.Value;
        LeftFork = tableManager.GetLeftFork(id);
        RightFork = tableManager.GetRightFork(id);
        Random = new Random();
        State = PhilosopherState.Thinking;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            LogState(null);
            
            while (!stoppingToken.IsCancellationRequested) {
                UpdateMetricsState();

                switch (State) {
                    case PhilosopherState.Thinking:
                        await ThinkAsync(stoppingToken);
                        break;
                    case PhilosopherState.Hungry:
                        await TryToEatAsync(stoppingToken);
                        break;
                    case PhilosopherState.Eating:
                        await EatAsync(stoppingToken);
                        break;
                }
            }
        }
        catch (OperationCanceledException) {
            // Expected when stopping
        }
        finally {
            ReleaseForks();
        }
    }

    private async Task ThinkAsync(CancellationToken stoppingToken) {
        var thinkingTime = Random.Next(Options.ThinkingTimeMin, Options.ThinkingTimeMax + 1);
        await Task.Delay(thinkingTime, stoppingToken);

        State = PhilosopherState.Hungry;
        HungryStartTime = DateTime.UtcNow;
        LogState(null);
    }

    private async Task TryToEatAsync(CancellationToken stoppingToken) {
        var action = Strategy.DecideAction(LeftFork, RightFork, State, HasLeftFork, HasRightFork);

        switch (action) {
            case PhilosopherAction.TakeLeftFork:
                if (!HasLeftFork) {
                    await Task.Delay(Options.ForkAcquisitionTime, stoppingToken);
                    if (LeftFork.TryTake(Id)) {
                        HasLeftFork = true;
                        LogState(action);
                        LogForkState(LeftFork);
                    }
                }
                break;

            case PhilosopherAction.TakeRightFork:
                if (!HasRightFork) {
                    await Task.Delay(Options.ForkAcquisitionTime, stoppingToken);
                    if (RightFork.TryTake(Id)) {
                        HasRightFork = true;
                        LogState(action);
                        LogForkState(RightFork);
                    }
                }
                break;

            case PhilosopherAction.ReleaseLeftFork:
                if (HasLeftFork) {
                    LeftFork.Release();
                    HasLeftFork = false;
                    LogState(action);
                    LogForkState(LeftFork);
                }
                break;

            case PhilosopherAction.ReleaseRightFork:
                if (HasRightFork) {
                    RightFork.Release();
                    HasRightFork = false;
                    LogState(action);
                    LogForkState(RightFork);
                }
                break;

            case PhilosopherAction.ReleaseBothForks:
                ReleaseForks();
                LogState(action);
                break;

            case PhilosopherAction.None:
                break;
        }

        if (HasLeftFork && HasRightFork) {
            State = PhilosopherState.Eating;
            var eatingStartTime = DateTime.UtcNow;
            var waitingTime = (long)(eatingStartTime - HungryStartTime).TotalMilliseconds;
            MetricsCollector.RecordWaitingTime(Id, waitingTime);

            LeftFork.MarkAsEating();
            RightFork.MarkAsEating();
            LogState(null);
        }
    }

    private async Task EatAsync(CancellationToken stoppingToken) {
        var eatingTime = Random.Next(Options.EatingTimeMin, Options.EatingTimeMax + 1);

        await Task.Delay(eatingTime, stoppingToken);

        MetricsCollector.RecordMeal(Id);

        ReleaseForks();
        
        State = PhilosopherState.Thinking;
        LogState(null);
    }

    private void ReleaseForks() {
        if (HasLeftFork) {
            LeftFork.Release();
            HasLeftFork = false;
            LogForkState(LeftFork);
        }

        if (HasRightFork) {
            RightFork.Release();
            HasRightFork = false;
            LogForkState(RightFork);
        }
    }

    private void UpdateMetricsState() {
        var metrics = MetricsCollector.GetMetrics(Id);
        metrics.PhilosopherName = Name;
        metrics.CurrentState = State;
        metrics.HasLeftFork = HasLeftFork;
        metrics.HasRightFork = HasRightFork;
    }

    private void LogState(PhilosopherAction? action) {
        var metrics = MetricsCollector.GetMetrics(Id);
        metrics.LastAction = action;
        StateLogger.LogPhilosopherState(Id, Name, State, HasLeftFork, HasRightFork, metrics.EatenCount, action);
    }
    
    private void LogForkState(Fork fork) {
        string? philosopherName = fork.UsedByPhilosopher.HasValue 
            ? MetricsCollector.GetMetrics(fork.UsedByPhilosopher.Value).PhilosopherName 
            : null;
        StateLogger.LogForkState(fork, philosopherName);
    }
}

