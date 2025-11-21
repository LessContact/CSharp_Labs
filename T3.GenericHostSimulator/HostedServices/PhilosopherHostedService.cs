using DiningPhilosophers.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using T3.GenericHostSimulator.Models;
using T3.GenericHostSimulator.Services;

namespace T3.GenericHostSimulator.HostedServices;

public abstract class PhilosopherHostedService : BackgroundService {
    protected readonly int Id;
    protected readonly string Name;
    protected readonly Fork LeftFork;
    protected readonly Fork RightFork;
    protected readonly IStrategy Strategy;
    protected readonly IMetricsCollector MetricsCollector;
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
        IStrategy strategy,
        IOptions<SimulationOptions> options) {
        Id = id;
        Name = name;
        MetricsCollector = metricsCollector;
        Strategy = strategy;
        Options = options.Value;
        LeftFork = tableManager.GetLeftFork(id);
        RightFork = tableManager.GetRightFork(id);
        Random = new Random(); // add seed for testing if needed
        State = PhilosopherState.Thinking;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
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
    }

    private async Task TryToEatAsync(CancellationToken stoppingToken) {
        var action = Strategy.DecideAction(LeftFork, RightFork, State, HasLeftFork, HasRightFork);

        switch (action) {
            case PhilosopherAction.TakeLeftFork:
                if (!HasLeftFork) {
                    await Task.Delay(Options.ForkAcquisitionTime, stoppingToken);
                    if (LeftFork.TryTake(Id)) HasLeftFork = true;
                }

                break;

            case PhilosopherAction.TakeRightFork:
                if (!HasRightFork) {
                    await Task.Delay(Options.ForkAcquisitionTime, stoppingToken);
                    if (RightFork.TryTake(Id)) HasRightFork = true;
                }

                break;

            case PhilosopherAction.ReleaseLeftFork:
                if (HasLeftFork) {
                    LeftFork.Release();
                    HasLeftFork = false;
                }

                break;

            case PhilosopherAction.ReleaseRightFork:
                if (HasRightFork) {
                    RightFork.Release();
                    HasRightFork = false;
                }

                break;

            case PhilosopherAction.ReleaseBothForks:
                ReleaseForks();
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
        }
    }

    private async Task EatAsync(CancellationToken stoppingToken) {
        var eatingTime = Random.Next(Options.EatingTimeMin, Options.EatingTimeMax + 1);

        await Task.Delay(eatingTime, stoppingToken);

        MetricsCollector.RecordMeal(Id);

        ReleaseForks();

        State = PhilosopherState.Thinking;
    }

    private void ReleaseForks() {
        if (HasLeftFork) {
            LeftFork.Release();
            HasLeftFork = false;
        }

        if (HasRightFork) {
            RightFork.Release();
            HasRightFork = false;
        }
    }

    private void UpdateMetricsState() {
        var metrics = MetricsCollector.GetMetrics(Id);
        metrics.PhilosopherName = Name;
        metrics.CurrentState = State;
        metrics.HasLeftFork = HasLeftFork;
        metrics.HasRightFork = HasRightFork;
    }
}