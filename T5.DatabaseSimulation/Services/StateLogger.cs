using System.Collections.Concurrent;
using System.Diagnostics;
using DatabaseSimulation.Data;
using DatabaseSimulation.Data.Entities;
using DatabaseSimulation.Models;
using DiningPhilosophers.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSimulation.Services;

public interface IStateLogger {
    int RunId { get; }
    void Initialize(string strategyName, int philosopherCount);
    void LogPhilosopherState(int philosopherId, string name, PhilosopherState state, 
        bool hasLeftFork, bool hasRightFork, int eatenCount, PhilosopherAction? action);
    void LogForkState(Fork fork, string? philosopherName);
    Task FlushAsync();
    Task CompleteAsync();
}

public class StateLogger : IStateLogger, IDisposable {
    private readonly IDbContextFactory<SimulationDbContext> _contextFactory;
    private readonly Stopwatch _stopwatch;
    private readonly ConcurrentQueue<PhilosopherStateLog> _philosopherLogs = new();
    private readonly ConcurrentQueue<ForkStateLog> _forkLogs = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _flushTask;

    public int RunId { get; private set; }
    private bool _initialized;
    private bool _completed;

    public StateLogger(IDbContextFactory<SimulationDbContext> contextFactory) {
        _contextFactory = contextFactory;
        _stopwatch = new Stopwatch();
        _flushTask = Task.Run(FlushLoopAsync);
    }

    public void Initialize(string strategyName, int philosopherCount) {
        using var context = _contextFactory.CreateDbContext();
        
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = strategyName,
            PhilosopherCount = philosopherCount
        };
        
        context.SimulationRuns.Add(run);
        context.SaveChanges();
        
        RunId = run.Id;
        _initialized = true;
        _stopwatch.Start();
        
        Console.WriteLine($"Симуляция инициализирована. RunId: {RunId}");
    }

    public void LogPhilosopherState(int philosopherId, string name, PhilosopherState state, 
        bool hasLeftFork, bool hasRightFork, int eatenCount, PhilosopherAction? action) {
        if (!_initialized || _completed) return;
        
        var log = new PhilosopherStateLog {
            SimulationRunId = RunId,
            PhilosopherId = philosopherId,
            PhilosopherName = name,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            State = state,
            HasLeftFork = hasLeftFork,
            HasRightFork = hasRightFork,
            EatenCount = eatenCount,
            Action = action
        };
        
        _philosopherLogs.Enqueue(log);
    }

    public void LogForkState(Fork fork, string? philosopherName) {
        if (!_initialized || _completed) return;
        
        var log = new ForkStateLog {
            SimulationRunId = RunId,
            ForkId = fork.Id,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            State = fork.State,
            UsedByPhilosopherId = fork.UsedByPhilosopher,
            UsedByPhilosopherName = philosopherName
        };
        
        _forkLogs.Enqueue(log);
    }

    private async Task FlushLoopAsync() {
        while (!_cts.Token.IsCancellationRequested) {
            try {
                await Task.Delay(100, _cts.Token);
                await FlushAsync();
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error flushing logs: {ex.Message}");
            }
        }
    }

    public async Task FlushAsync() {
        if (!_initialized) return;
        
        var philosopherLogs = new List<PhilosopherStateLog>();
        var forkLogs = new List<ForkStateLog>();
        
        while (_philosopherLogs.TryDequeue(out var pLog))
            philosopherLogs.Add(pLog);
        
        while (_forkLogs.TryDequeue(out var fLog))
            forkLogs.Add(fLog);
        
        if (philosopherLogs.Count == 0 && forkLogs.Count == 0) return;
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        if (philosopherLogs.Count > 0)
            await context.PhilosopherStateLogs.AddRangeAsync(philosopherLogs);
        
        if (forkLogs.Count > 0)
            await context.ForkStateLogs.AddRangeAsync(forkLogs);
        
        await context.SaveChangesAsync();
    }

    public async Task CompleteAsync() {
        if (!_initialized || _completed) return;
        
        _completed = true;
        _stopwatch.Stop();
        
        await FlushAsync();
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var run = await context.SimulationRuns.FindAsync(RunId);
        if (run != null) {
            run.EndTime = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        
        Console.WriteLine($"\nСимуляция завершена. RunId: {RunId}");
        Console.WriteLine($"Длительность: {_stopwatch.ElapsedMilliseconds} мс");
    }

    public void Dispose() {
        _cts.Cancel();
        try {
            _flushTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch { }
        _cts.Dispose();
    }
}

