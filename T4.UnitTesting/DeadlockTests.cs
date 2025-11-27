using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using T1.SynchronousSimulation;
using T1.SynchronousSimulation.Models;

namespace T4.UnitTesting;

public class DeadlockTests {
    [Fact]
    public void Simulation_ShouldDetect_DeadlockWithNaiveStrategy() {
        var philosopherNames = new List<string> { "P1", "P2", "P3", "P4", "P5" };
        
        const int seed = 322;
        
        var simulation = CreateSimulationWithNaiveStrategy(philosopherNames, seed);
        
        var isDeadlocked = simulation.Run();
        
        Assert.True(isDeadlocked);
    }
    
    [Fact]
    public void ManualDetectDeadlock_WhenAllPhilosophers_HoldOneFork() {
        var philosophers = new List<Philosopher>();
        var forks = new List<Fork>();
        
        for (int i = 0; i < 5; i++) {
            forks.Add(new Fork(i));
        }
        
        for (int i = 0; i < 5; i++) {
            var leftFork = forks[i];
            var rightFork = forks[(i + 1) % 5];
            var strategy = new NaiveStrategy();
            var random = new Random(322);
            
            var philosopher = new Philosopher(i, $"Philosopher{i}", 
                leftFork, rightFork, strategy, null, random);
            
            philosophers.Add(philosopher);
        }
        
        for (int i = 0; i < 11; i++) {
            foreach (var p in philosophers) {
                p.Step();
            }
        }
        
        foreach (var p in philosophers) {
            p.Step();
        }
        
        foreach (var p in philosophers) {
            p.Step();
        }
        
        foreach (var p in philosophers) {
            p.Step();
        }
        
        int hungryWithOneFork = 0;
        foreach (var philosopher in philosophers) {
            if (philosopher.State == PhilosopherState.Hungry && 
                philosopher.HasLeftFork && !philosopher.HasRightFork) {
                hungryWithOneFork++;
            }
        }
        
        Assert.Equal(5, hungryWithOneFork);
        
        Assert.All(forks, f => Assert.Equal(ForkState.InUse, f.State));
    }
    
    [Fact]
    public void Coordinator_ShouldPrevent_Deadlock() {
        int philosopherCount = 5;
        var coordinator = new Coordinator(philosopherCount);
        var philosopherNames = new List<string> { "P1", "P2", "P3", "P4", "P5" };

        const int seed = 322;
        
        var simulation = CreateSimulationWithCoordinator(philosopherNames, coordinator, seed);
        
        var isDeadlocked = simulation.Run();
        
        Assert.False(isDeadlocked);
    }
    
    [Fact]
    public void HierarchyStrategy_ShouldPrevent_Deadlock() {
        var philosopherNames = new List<string> { "P1", "P2", "P3", "P4", "P5" };

        const int seed = 322;
        
        var simulation = CreateSimulationWithHierarchyStrategy(philosopherNames, seed);
        
        var isDeadlocked = simulation.Run();
        
        Assert.False(isDeadlocked);
    }
    
    private static Simulation CreateSimulationWithNaiveStrategy(List<string> names, int seed) {
        return new Simulation(
            names,
            _ => new NaiveStrategy(),
            null,
            totalSteps: 100,
            displayInterval: 1000,
            seed: seed
        );
    }

    private static Simulation CreateSimulationWithCoordinator(List<string> names, Coordinator coordinator, int seed) {
        return new Simulation(
            names,
            _ => new CoordinatorStrategy(),
            coordinator,
            totalSteps: 100,
            displayInterval: 1000,
            seed: seed
        );
    }

    private static Simulation CreateSimulationWithHierarchyStrategy(List<string> names, int seed) {
        return new Simulation(
            names,
            _ => new HierarchyStrategy(),
            null,
            totalSteps: 100,
            displayInterval: 1000,
            seed: seed
        );
    }
}

