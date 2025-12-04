using DatabaseSimulation.Data;
using DatabaseSimulation.Data.Entities;
using DiningPhilosophers.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace T4.UnitTesting;

public class DatabaseTests : IDisposable {
    private readonly SqliteConnection _connection;
    private readonly SimulationDbContext _context;

    public DatabaseTests() {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SimulationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SimulationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose() {
        _context.Dispose();
        _connection.Close();
    }

    [Fact]
    public void SimulationRun_CanBeCreatedAndRetrieved() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();
        
        var retrieved = _context.SimulationRuns.Find(run.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("TestStrategy", retrieved.StrategyName);
        Assert.Equal(5, retrieved.PhilosopherCount);
    }

    [Fact]
    public void PhilosopherStateLog_CanBeCreatedWithSimulationRun() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();

        var log = new PhilosopherStateLog {
            SimulationRunId = run.Id,
            PhilosopherId = 0,
            PhilosopherName = "Аристотель",
            TimestampMs = 100,
            State = PhilosopherState.Hungry,
            HasLeftFork = false,
            HasRightFork = false,
            EatenCount = 0,
            Action = PhilosopherAction.TakeLeftFork
        };
        
        _context.PhilosopherStateLogs.Add(log);
        _context.SaveChanges();
        
        var retrieved = _context.PhilosopherStateLogs.First(l => l.SimulationRunId == run.Id);
        Assert.Equal("Аристотель", retrieved.PhilosopherName);
        Assert.Equal(PhilosopherState.Hungry, retrieved.State);
        Assert.Equal(PhilosopherAction.TakeLeftFork, retrieved.Action);
    }

    [Fact]
    public void ForkStateLog_CanBeCreatedWithSimulationRun() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();

        var log = new ForkStateLog {
            SimulationRunId = run.Id,
            ForkId = 0,
            TimestampMs = 100,
            State = ForkState.InUse,
            UsedByPhilosopherId = 0,
            UsedByPhilosopherName = "Аристотель"
        };
        
        _context.ForkStateLogs.Add(log);
        _context.SaveChanges();
        
        var retrieved = _context.ForkStateLogs.First(l => l.SimulationRunId == run.Id);
        Assert.Equal(ForkState.InUse, retrieved.State);
        Assert.Equal("Аристотель", retrieved.UsedByPhilosopherName);
    }

    [Fact]
    public void CanQueryStateAtSpecificTime() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();
        
        var logs = new[] {
            new PhilosopherStateLog {
                SimulationRunId = run.Id,
                PhilosopherId = 0,
                PhilosopherName = "Аристотель",
                TimestampMs = 0,
                State = PhilosopherState.Thinking,
                HasLeftFork = false,
                HasRightFork = false,
                EatenCount = 0
            },
            new PhilosopherStateLog {
                SimulationRunId = run.Id,
                PhilosopherId = 0,
                PhilosopherName = "Аристотель",
                TimestampMs = 100,
                State = PhilosopherState.Hungry,
                HasLeftFork = false,
                HasRightFork = false,
                EatenCount = 0
            },
            new PhilosopherStateLog {
                SimulationRunId = run.Id,
                PhilosopherId = 0,
                PhilosopherName = "Аристотель",
                TimestampMs = 150,
                State = PhilosopherState.Hungry,
                HasLeftFork = true,
                HasRightFork = false,
                EatenCount = 0
            },
            new PhilosopherStateLog {
                SimulationRunId = run.Id,
                PhilosopherId = 0,
                PhilosopherName = "Аристотель",
                TimestampMs = 200,
                State = PhilosopherState.Eating,
                HasLeftFork = true,
                HasRightFork = true,
                EatenCount = 0
            }
        };

        _context.PhilosopherStateLogs.AddRange(logs);
        _context.SaveChanges();
        
        var targetTime = 175L;
        var stateAtTime = _context.PhilosopherStateLogs
            .Where(l => l.SimulationRunId == run.Id 
                        && l.PhilosopherId == 0 
                        && l.TimestampMs <= targetTime)
            .OrderByDescending(l => l.TimestampMs)
            .FirstOrDefault();
        
        Assert.NotNull(stateAtTime);
        Assert.Equal(150, stateAtTime.TimestampMs);
        Assert.Equal(PhilosopherState.Hungry, stateAtTime.State);
        Assert.True(stateAtTime.HasLeftFork);
        Assert.False(stateAtTime.HasRightFork);
    }

    [Fact]
    public void SimulationRun_CascadeDeletesLogs() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();

        _context.PhilosopherStateLogs.Add(new PhilosopherStateLog {
            SimulationRunId = run.Id,
            PhilosopherId = 0,
            PhilosopherName = "Аристотель",
            TimestampMs = 0,
            State = PhilosopherState.Thinking,
            EatenCount = 0
        });

        _context.ForkStateLogs.Add(new ForkStateLog {
            SimulationRunId = run.Id,
            ForkId = 0,
            TimestampMs = 0,
            State = ForkState.Available
        });

        _context.SaveChanges();
        
        _context.SimulationRuns.Remove(run);
        _context.SaveChanges();
        
        Assert.Empty(_context.SimulationRuns);
        Assert.Empty(_context.PhilosopherStateLogs);
        Assert.Empty(_context.ForkStateLogs);
    }

    [Fact]
    public void CanGetAllPhilosophersStateAtSpecificTime() {
        var run = new SimulationRun {
            StartTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            PhilosopherCount = 5
        };
        _context.SimulationRuns.Add(run);
        _context.SaveChanges();

        var philosopherNames = new[] { "Аристотель", "Платон", "Сократ", "Декарт", "Кант" };
        
        for (var i = 0; i < 5; i++) {
            _context.PhilosopherStateLogs.Add(new PhilosopherStateLog {
                SimulationRunId = run.Id,
                PhilosopherId = i,
                PhilosopherName = philosopherNames[i],
                TimestampMs = 0,
                State = PhilosopherState.Thinking,
                HasLeftFork = false,
                HasRightFork = false,
                EatenCount = 0
            });
        }
        
        _context.PhilosopherStateLogs.Add(new PhilosopherStateLog {
            SimulationRunId = run.Id,
            PhilosopherId = 0,
            PhilosopherName = philosopherNames[0],
            TimestampMs = 50,
            State = PhilosopherState.Hungry,
            HasLeftFork = false,
            HasRightFork = false,
            EatenCount = 0
        });
        
        _context.PhilosopherStateLogs.Add(new PhilosopherStateLog {
            SimulationRunId = run.Id,
            PhilosopherId = 1,
            PhilosopherName = philosopherNames[1],
            TimestampMs = 75,
            State = PhilosopherState.Eating,
            HasLeftFork = true,
            HasRightFork = true,
            EatenCount = 1
        });
        
        _context.SaveChanges();
        
        var targetTime = 100L;
        var allLogs = _context.PhilosopherStateLogs
            .Where(l => l.SimulationRunId == run.Id && l.TimestampMs <= targetTime)
            .ToList();
        
        var statesAtTime = allLogs
            .GroupBy(l => l.PhilosopherId)
            .Select(g => g.OrderByDescending(l => l.TimestampMs).First())
            .OrderBy(l => l.PhilosopherId)
            .ToList();
        
        Assert.Equal(5, statesAtTime.Count);
        Assert.Equal(PhilosopherState.Hungry, statesAtTime[0].State);  // Philosopher 0 at 50ms
        Assert.Equal(PhilosopherState.Eating, statesAtTime[1].State);   // Philosopher 1 at 75ms
        Assert.Equal(PhilosopherState.Thinking, statesAtTime[2].State); // Philosopher 2 at 0ms
        Assert.Equal(PhilosopherState.Thinking, statesAtTime[3].State); // Philosopher 3 at 0ms
        Assert.Equal(PhilosopherState.Thinking, statesAtTime[4].State); // Philosopher 4 at 0ms
    }
}

