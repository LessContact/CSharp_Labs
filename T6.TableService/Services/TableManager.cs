using System.Diagnostics;
using TableService.Models;
using DiningPhilosophers.Contracts;

namespace TableService.Services;

/// <summary>
/// Менеджер стола - управляет вилками и философами
/// </summary>
public interface ITableManager {
    Fork GetFork(int forkId);
    IReadOnlyList<Fork> GetAllForks();
    bool RegisterPhilosopher(string philosopherId, string philosopherName, int leftForkId, int rightForkId);
    void UpdatePhilosopherState(string philosopherId, PhilosopherState state, bool hasLeftFork, bool hasRightFork, int eatenCount);
    void RecordMeal(string philosopherId, string philosopherName, long waitingTimeMs);
    void PhilosopherExit(string philosopherId, string philosopherName, int totalMeals);
    SimulationStatusResponse GetStatus();
    SimulationMetrics GetFinalMetrics();
    bool AllPhilosophersExited();
    int GetPhilosophersCount();
    int GetExpectedPhilosophersCount();
}

public class TableManager : ITableManager {
    private readonly List<Fork> _forks;
    private readonly Dictionary<string, PhilosopherInfo> _philosophers = new();
    private readonly Lock _lock = new();
    private readonly Stopwatch _stopwatch;
    private readonly int _expectedPhilosophersCount;
    private readonly ILogger<TableManager> _logger;

    public TableManager(int philosophersCount, ILogger<TableManager> logger) {
        _expectedPhilosophersCount = philosophersCount;
        _logger = logger;
        _forks = new List<Fork>();
        for (var i = 0; i < philosophersCount; i++) {
            _forks.Add(new Fork(i));
        }
        _stopwatch = Stopwatch.StartNew();
    }

    public Fork GetFork(int forkId) {
        if (forkId < 0 || forkId >= _forks.Count) {
            throw new ArgumentOutOfRangeException(nameof(forkId), $"Fork {forkId} does not exist");
        }
        return _forks[forkId];
    }

    public IReadOnlyList<Fork> GetAllForks() {
        return _forks.AsReadOnly();
    }

    public bool RegisterPhilosopher(string philosopherId, string philosopherName, int leftForkId, int rightForkId) {
        lock (_lock) {
            if (_philosophers.ContainsKey(philosopherId)) {
                _logger.LogWarning("Philosopher {PhilosopherId} already registered", philosopherId);
                return false;
            }

            _philosophers[philosopherId] = new PhilosopherInfo {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                LeftForkId = leftForkId,
                RightForkId = rightForkId
            };

            _logger.LogInformation("Philosopher {Name} ({Id}) registered with forks L:{LeftFork}, R:{RightFork}", 
                philosopherName, philosopherId, leftForkId, rightForkId);
            return true;
        }
    }

    public void UpdatePhilosopherState(string philosopherId, PhilosopherState state, bool hasLeftFork, bool hasRightFork, int eatenCount) {
        lock (_lock) {
            if (!_philosophers.TryGetValue(philosopherId, out var philosopher)) return;

            philosopher.State = state;
            philosopher.HasLeftFork = hasLeftFork;
            philosopher.HasRightFork = hasRightFork;
            philosopher.EatenCount = eatenCount;
        }
    }

    public void RecordMeal(string philosopherId, string philosopherName, long waitingTimeMs) {
        lock (_lock) {
            if (!_philosophers.TryGetValue(philosopherId, out var philosopher)) {
                _logger.LogWarning("Meal recorded for unregistered philosopher {Id}", philosopherId);
                return;
            }

            philosopher.TotalWaitingMs += waitingTimeMs;
            philosopher.EatenCount++;
        }
    }

    public void PhilosopherExit(string philosopherId, string philosopherName, int totalMeals) {
        lock (_lock) {
            if (!_philosophers.TryGetValue(philosopherId, out var philosopher)) {
                _logger.LogWarning("Exit received from unregistered philosopher {Id}", philosopherId);
                return;
            }

            philosopher.HasExited = true;
            philosopher.EatenCount = totalMeals;
            _logger.LogInformation("Philosopher {Name} exited with {Meals} meals", philosopherName, totalMeals);
        }
    }

    public SimulationStatusResponse GetStatus() {
        lock (_lock) {
            return new SimulationStatusResponse {
                Philosophers = _philosophers.Values
                    .OrderBy(p => p.PhilosopherId)
                    .Select(p => p.ToStatusInfo())
                    .ToList(),
                Forks = _forks.Select(f => f.ToForkInfo()).ToList(),
                ElapsedMs = _stopwatch.ElapsedMilliseconds,
                IsRunning = !AllPhilosophersExited()
            };
        }
    }

    public SimulationMetrics GetFinalMetrics() {
        lock (_lock) {
            var totalMs = _stopwatch.ElapsedMilliseconds;
            var philosophers = _philosophers.Values.ToList();
            
            var totalMeals = philosophers.Sum(p => p.EatenCount);
            var totalWaitingMs = philosophers.Sum(p => p.TotalWaitingMs);
            
            var maxWaiting = philosophers.OrderByDescending(p => p.TotalWaitingMs).FirstOrDefault();
            
            var metrics = new SimulationMetrics {
                TotalMeals = totalMeals,
                TotalDurationMs = totalMs,
                AverageThroughput = totalMs > 0 ? totalMeals / (double)totalMs : 0,
                AverageWaitingTimeMs = totalMeals > 0 ? totalWaitingMs / (double)totalMeals : 0,
                MaxWaitingTimeMs = maxWaiting?.TotalWaitingMs ?? 0,
                MaxWaitingPhilosopher = maxWaiting?.PhilosopherName,
                PhilosopherMetrics = philosophers.Select(p => new PhilosopherMetricInfo {
                    PhilosopherId = p.PhilosopherId,
                    PhilosopherName = p.PhilosopherName,
                    EatenCount = p.EatenCount,
                    Throughput = totalMs > 0 ? p.EatenCount / (double)totalMs : 0,
                    TotalWaitingMs = p.TotalWaitingMs,
                    AverageWaitingMs = p.EatenCount > 0 ? p.TotalWaitingMs / (double)p.EatenCount : 0
                }).ToList(),
                ForkUtilizations = _forks.Select(f => {
                    var (available, blocked, eating) = f.GetUtilizationPercent(totalMs);
                    return new ForkUtilizationInfo {
                        ForkId = f.Id,
                        AvailablePercent = available,
                        BlockedPercent = blocked,
                        EatingPercent = eating
                    };
                }).ToList()
            };
            
            return metrics;
        }
    }

    public bool AllPhilosophersExited() {
        lock (_lock) {
            return _philosophers.Count >= _expectedPhilosophersCount 
                   && _philosophers.Values.All(p => p.HasExited);
        }
    }

    public int GetPhilosophersCount() {
        lock (_lock) {
            return _philosophers.Count;
        }
    }

    public int GetExpectedPhilosophersCount() {
        return _expectedPhilosophersCount;
    }
}

