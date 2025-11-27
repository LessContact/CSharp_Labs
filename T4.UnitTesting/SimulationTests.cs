using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using T1.SynchronousSimulation.Models;

namespace T4.UnitTesting;

public class ForkTests {
    [Fact]
    public void Fork_InitialState_ShouldBeAvailable() {
        var fork = new Fork(0);
        
        Assert.Equal(ForkState.Available, fork.State);
        Assert.Null(fork.UsedByPhilosopher);
    }
    
    [Fact]
    public void Fork_Take_ShouldSucceed_WhenAvailable() {
        var fork = new Fork(0);
        const int philosopherId = 1;
        
        var result = fork.Take(philosopherId);
        
        Assert.True(result);
        Assert.Equal(ForkState.InUse, fork.State);
        Assert.Equal(philosopherId, fork.UsedByPhilosopher);
    }

    [Fact]
    public void Fork_Take_ShouldFail_WhenOccupied() {
        var fork = new Fork(0);
        fork.Take(1);
        
        bool result = fork.Take(2);
        
        Assert.False(result);
        Assert.Equal(ForkState.InUse, fork.State);
        Assert.Equal(1, fork.UsedByPhilosopher);
    }
    
    [Fact]
    public void Fork_Release_ShouldMakeAvailable() {
        var fork = new Fork(0);
        fork.Take(1);
        
        fork.Release();
        
        Assert.Equal(ForkState.Available, fork.State);
        Assert.Null(fork.UsedByPhilosopher);
    }
    
    [Fact]
    public void Fork_TakeAndRelease_Cycle() {
        var fork = new Fork(0);
        
        Assert.True(fork.Take(1));
        Assert.Equal(ForkState.InUse, fork.State);
        
        fork.Release();
        Assert.Equal(ForkState.Available, fork.State);
        
        
        Assert.True(fork.Take(2));
        Assert.Equal(ForkState.InUse, fork.State);
        Assert.Equal(2, fork.UsedByPhilosopher);
        
        fork.Release();
        Assert.Equal(ForkState.Available, fork.State);
    }
}

public class SimulationIntegrationTests {
    [Fact]
    public void Simulation_FullCycle_ThinkingToEatingToThinking() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var strategy = new NaiveStrategy();
        var random = new Random(322);
        
        var philosopher = new Philosopher(0, "TestPhilosopher", 
            leftFork, rightFork, strategy, null, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        Assert.Equal(PhilosopherState.Hungry, philosopher.State);
        
        for (int i = 0; i < 10; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Eating)
                break;
        }
        Assert.Equal(PhilosopherState.Eating, philosopher.State);
        Assert.Equal(1, philosopher.EatenCount);
        
        for (int i = 0; i < 6; i++) {
            philosopher.Step();
        }
        
        Assert.Equal(PhilosopherState.Thinking, philosopher.State);
        Assert.False(philosopher.HasLeftFork);
        Assert.False(philosopher.HasRightFork);
        Assert.Equal(ForkState.Available, leftFork.State);
        Assert.Equal(ForkState.Available, rightFork.State);
    }
    
    [Fact]
    public void Simulation_TwoPhilosophers_CompeteForSharedFork() {
        // Arrange
        var fork0 = new Fork(0);
        var fork1 = new Fork(1);
        var fork2 = new Fork(2);
        
        var philosopher0 = new Philosopher(0, "P0", fork0, fork1, 
            new NaiveStrategy(), null, new Random(322));
        var philosopher1 = new Philosopher(1, "P1", fork1, fork2, 
            new NaiveStrategy(), null, new Random(322));
        
        for (int i = 0; i < 11; i++) {
            philosopher0.Step();
            if (philosopher0.State == PhilosopherState.Hungry)
                break;
        }
        Assert.Equal(PhilosopherState.Hungry, philosopher0.State);
        
        philosopher0.Step(); 
        philosopher0.Step();
        philosopher0.Step();
        Assert.True(philosopher0.HasLeftFork);
        Assert.Equal(ForkState.InUse, fork0.State);
        
        philosopher0.Step();
        philosopher0.Step();
        philosopher0.Step();
        Assert.True(philosopher0.HasRightFork);
        Assert.Equal(ForkState.InUse, fork1.State);
        Assert.Equal(0, fork1.UsedByPhilosopher);
        
        philosopher0.Step();
        Assert.Equal(PhilosopherState.Eating, philosopher0.State);
        
        for (int i = 0; i < 11; i++) {
            philosopher1.Step();
            if (philosopher1.State == PhilosopherState.Hungry)
                break;
        }
        Assert.Equal(PhilosopherState.Hungry, philosopher1.State);
        
        philosopher1.Step();
        
        Assert.Equal(ForkState.InUse, fork1.State);
        Assert.Equal(0, fork1.UsedByPhilosopher); // все еще занята философом 0
        Assert.False(philosopher1.HasLeftFork);
        Assert.Equal(PhilosopherState.Hungry, philosopher1.State);
    }
    
    [Fact]
    public void Simulation_ShouldTrack_MetricsCorrectly() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var strategy = new NaiveStrategy();
        var random = new Random(322);
        
        var philosopher = new Philosopher(0, "TestPhilosopher", 
            leftFork, rightFork, strategy, null, random);
        
        int initialEatenCount = philosopher.EatenCount;
        
        for (int i = 0; i < 30; i++) {
            philosopher.Step();
        }
        
        Assert.True(philosopher.EatenCount > initialEatenCount);
    }
    
    [Fact]
    public void Simulation_WithMultiplePhilosophers_ShouldWork() {
        var philosopherNames = new List<string> { "Plato", "Aristotle", "Socrates" };
        var forks = new List<Fork>();
        var philosophers = new List<Philosopher>();
        
        for (int i = 0; i < philosopherNames.Count; i++) {
            forks.Add(new Fork(i));
        }
        
        for (int i = 0; i < philosopherNames.Count; i++) {
            var leftFork = forks[i];
            var rightFork = forks[(i + 1) % philosopherNames.Count];
            var strategy = new NaiveStrategy();
            var random = new Random();
            
            var philosopher = new Philosopher(i, philosopherNames[i], 
                leftFork, rightFork, strategy, null, random);
            
            philosophers.Add(philosopher);
        }
        
        for (int step = 0; step < 500; step++) {
            foreach (var philosopher in philosophers) {
                philosopher.Step();
            }
        }
        
        int totalEaten = philosophers.Sum(p => p.EatenCount);
        Assert.True(totalEaten > 0);
    }

    [Fact]
    public void Simulation_ShouldAccumulate_HungryStepsAcrossMultipleCycles() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var strategy = new NaiveStrategy();
        var random = new Random(42);
        
        var philosopher = new Philosopher(0, "TestPhilosopher", 
            leftFork, rightFork, strategy, null, random);
        
        for (int cycle = 0; cycle < 3; cycle++) {
            for (int i = 0; i < 11; i++) {
                philosopher.Step();
                if (philosopher.State == PhilosopherState.Hungry)
                    break;
            }
            
            for (int i = 0; i < 10; i++) {
                philosopher.Step();
                if (philosopher.State == PhilosopherState.Eating)
                    break;
            }
            
            for (int i = 0; i < 6; i++) {
                philosopher.Step();
            }
        }
        
        Assert.Equal(3, philosopher.EatenCount);
        Assert.True(philosopher.TotalHungrySteps > 0);
    }
}

