using System.Diagnostics;
using System.Text;
using DiningPhilosophers.Contracts;
using T2.MultithreadedSimulation.Models;

namespace T2.MultithreadedSimulation;

public class Simulation {
    private readonly List<Philosopher> _philosophers;
    private readonly List<Fork> _forks;
    private readonly int _durationMs;
    private readonly int _displayIntervalMs;
    private readonly CancellationTokenSource _cts;

    public Simulation(List<string> philosopherNames, Func<int, IStrategy> strategyFactory, 
        int durationMs, int displayIntervalMs = 150) {
        _durationMs = durationMs;
        _displayIntervalMs = displayIntervalMs;
        _cts = new CancellationTokenSource();
        
        _forks = new List<Fork>();
        for (var i = 0; i < philosopherNames.Count; i++) {
            _forks.Add(new Fork(i));
        }
        
        _philosophers = new List<Philosopher>();
        for (var i = 0; i < philosopherNames.Count; i++) {
            var leftFork = _forks[i];
            var rightFork = _forks[(i + 1) % philosopherNames.Count];
            var strategy = strategyFactory(i);
            var random = new Random(); // add seed if needed

            _philosophers.Add(new Philosopher(i, philosopherNames[i], leftFork, rightFork, 
                strategy, random, _cts.Token));
        }
    }

    public async Task RunAsync() {
        Console.WriteLine($"Запуск многопоточной симуляции на {_durationMs} мс...\n");

        var stopwatch = Stopwatch.StartNew();
        
        var philosopherTasks = _philosophers.Select(p => Task.Run(p.RunAsync)).ToList();

        // monitoring task
        _ = Task.Run(async () => {
            while (!_cts.Token.IsCancellationRequested) {
                try {
                    await Task.Delay(_displayIntervalMs, _cts.Token);
                    DisplayState(stopwatch.ElapsedMilliseconds);
                } catch (OperationCanceledException) {
                    // expected exception
                }
            }
        });

        // wait for the simulation duration
        await Task.Delay(_durationMs);
        
        _cts.Cancel();

        try {
            await Task.WhenAll(philosopherTasks);
        } catch (OperationCanceledException) {
            // expected exception
        }

        stopwatch.Stop();
        
        Console.WriteLine("\n=== ФИНАЛЬНОЕ СОСТОЯНИЕ ===");
        DisplayState(stopwatch.ElapsedMilliseconds);
        
        var metrics = new Metrics(_philosophers, _forks, stopwatch.ElapsedMilliseconds);
        metrics.PrintMetrics();
    }

    private void DisplayState(long elapsedMs) {
        Console.WriteLine($"\n===== ВРЕМЯ: {elapsedMs} мс =====");
        Console.WriteLine("\nФилософы:");

        foreach (var philosopher in _philosophers) {
            var stateBuilder = new StringBuilder();
            stateBuilder.Append(' ');
            stateBuilder.Append(philosopher.Name);
            stateBuilder.Append($": {philosopher.State}");

            if (philosopher.HasLeftFork || philosopher.HasRightFork) {
                stateBuilder.Append(' ');
                stateBuilder.Append('(');
                if (philosopher.HasLeftFork) stateBuilder.Append('L');
                if (philosopher.HasRightFork) stateBuilder.Append('R');
                stateBuilder.Append(')');
            }

            stateBuilder.Append($", съедено: {philosopher.EatenCount}");
            Console.WriteLine(stateBuilder.ToString());
        }

        Console.WriteLine("\nВилки:");
        foreach (var fork in _forks) {
            var stateBuilder = new StringBuilder();
            stateBuilder.Append($"  Fork-{fork.Id + 1}: ");
            stateBuilder.Append(fork.State.ToString());

            if (fork is { State: ForkState.InUse, UsedByPhilosopher: not null }) {
                var philosopher = _philosophers[fork.UsedByPhilosopher.Value];
                stateBuilder.Append($" (используется {philosopher.Name})");
            }

            Console.WriteLine(stateBuilder.ToString());
        }
    }
}

