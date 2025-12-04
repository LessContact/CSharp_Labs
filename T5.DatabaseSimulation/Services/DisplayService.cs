using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DatabaseSimulation.Services;

public class DisplayService : BackgroundService {
    private readonly ITableManager _tableManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly SimulationOptions _options;
    private readonly Stopwatch _stopwatch;

    public DisplayService(
        ITableManager tableManager,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options) {
        _tableManager = tableManager;
        _metricsCollector = metricsCollector;
        _options = options.Value;
        _stopwatch = Stopwatch.StartNew();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(_options.DisplayUpdateInterval, stoppingToken);
                DisplayState();
            }
        }
        catch (OperationCanceledException) {
            // Expected when stopping
        }
    }

    private void DisplayState() {
        var elapsedMs = _stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"\n===== ВРЕМЯ: {elapsedMs} мс =====");
        Console.WriteLine("\nФилософы:");

        for (var i = 0; i < 5; i++) {
            var metrics = _metricsCollector.GetMetrics(i);
            var stateBuilder = new StringBuilder();
            stateBuilder.Append($"  {metrics.PhilosopherName}: {metrics.CurrentState}");

            if (metrics.HasLeftFork || metrics.HasRightFork) {
                stateBuilder.Append(' ');
                stateBuilder.Append('(');
                if (metrics.HasLeftFork) stateBuilder.Append('L');
                if (metrics.HasRightFork) stateBuilder.Append('R');
                stateBuilder.Append(')');
            }

            if (metrics.LastAction.HasValue) {
                stateBuilder.Append($" [Action: {metrics.LastAction}]");
            }

            stateBuilder.Append($", съедено: {metrics.EatenCount}");
            Console.WriteLine(stateBuilder.ToString());
        }

        Console.WriteLine("\nВилки:");
        var forks = _tableManager.GetAllForks();
        foreach (var fork in forks) {
            var stateBuilder = new StringBuilder();
            stateBuilder.Append($"  Fork-{fork.Id + 1}: {fork.State}");

            if (fork.UsedByPhilosopher.HasValue) {
                var metrics = _metricsCollector.GetMetrics(fork.UsedByPhilosopher.Value);
                stateBuilder.Append($" (используется {metrics.PhilosopherName})");
            }

            Console.WriteLine(stateBuilder.ToString());
        }
    }

    public void DisplayFinalState() {
        Console.WriteLine("\n=== ФИНАЛЬНОЕ СОСТОЯНИЕ ===");
        DisplayState();
        _metricsCollector.PrintFinalMetrics(_tableManager, _stopwatch.ElapsedMilliseconds);
    }
}

