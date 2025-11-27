using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using System.Text;
using T1.SynchronousSimulation.Models;

namespace T1.SynchronousSimulation;

public class Simulation {
    private readonly List<Philosopher> _philosophers;
    private readonly List<Fork> _forks;
    private readonly ICoordinator? _coordinator;
    private readonly Metrics _metrics;
    private readonly int _totalSteps;
    private readonly int _displayInterval;
    private readonly Dictionary<int, CoordinatorStrategy> _coordinatedStrategies = new();

    public Simulation(List<string> philosopherNames, Func<int, IStrategy> strategyFactory,
        ICoordinator? coordinator, int totalSteps, int displayInterval = 50000, int? seed = null) {
        _totalSteps = totalSteps;
        _displayInterval = displayInterval;
        _coordinator = coordinator;
        
        _forks = new List<Fork>();
        for (int i = 0; i < philosopherNames.Count; i++) {
            _forks.Add(new Fork(i));
        }
        
        _philosophers = new List<Philosopher>();
        for (int i = 0; i < philosopherNames.Count; i++) {
            var leftFork = _forks[i];
            var rightFork = _forks[(i + 1) % philosopherNames.Count];
            var strategy = strategyFactory(i);
            
            if (strategy is CoordinatorStrategy coordStrategy) {
                _coordinatedStrategies[i] = coordStrategy;
            }

            var setSeed = seed ?? Environment.TickCount + i * 1000;
            _philosophers.Add(new Philosopher(i, philosopherNames[i], leftFork, rightFork,
                strategy, coordinator, new Random(setSeed))); // set seed to easily see deadlocks
        }

        _metrics = new Metrics(_philosophers, _forks, totalSteps);

        if (_coordinator != null) {
            _coordinator.CommandToPhilosopher += OnCoordinatorCommand;
        }
    }

    private void OnCoordinatorCommand(object? sender, PhilosopherCommandEventArgs e) {
        // TODO: this should be done in philosophers not simulations probably
        if (_coordinatedStrategies.TryGetValue(e.PhilosopherId, out var strategy)) {
            strategy.SetPendingAction(e.Action);
        }
    }

    public bool Run() {
        Console.WriteLine($"Запуск симуляции на {_totalSteps} шагов...\n");
        var isDeadlocked = false;

        for (int step = 0; step < _totalSteps; step++) {
            // Console.ReadKey();

            _coordinator?.Step();

            foreach (var philosopher in _philosophers) {
                philosopher.Step();
            }
            
            _metrics.RecordForkUsage();

            if (IsDeadlocked()) {
                Console.WriteLine($"\n!!! ОБНАРУЖЕН DEADLOCK НА ШАГЕ {step} !!!");
                DisplayState(step);
                isDeadlocked = true;
                break;
            }
            
            if (step % _displayInterval == 0 || step == _totalSteps - 1) {
                DisplayState(step);
            }
        }

        _metrics.PrintMetrics();
        return isDeadlocked;
    }

    private bool IsDeadlocked() {
        // TODO: make better. what if the all take a fork and immediately release it? 
        // will it be detected? and what if it is a false positive?
        // TODO: make separate DeadlockDetector to make testing easier
        // Deadlock возникает когда все философы голодны и каждый держит только одну вилку
        int hungryWithOneFork = 0;

        foreach (var philosopher in _philosophers) {
            if (philosopher.State is PhilosopherState.Hungry && 
                philosopher is { HasLeftFork: true, HasRightFork: false } or 
                               { HasLeftFork: false, HasRightFork: true }) {
                hungryWithOneFork++;
            }
        }

        return hungryWithOneFork == _philosophers.Count;
    }

    private void DisplayState(int step) {
        Console.WriteLine($"\n===== ШАГ {step} =====");
        Console.WriteLine("\nФилософы:");

        foreach (var philosopher in _philosophers) {
            var stateBuilder = new StringBuilder();
            stateBuilder.Append(philosopher.State.ToString());

            if (philosopher.State is PhilosopherState.Eating or PhilosopherState.Thinking) {
                int stepsLeft = philosopher.GetStepsLeftInState();
                stateBuilder.Append($" ({stepsLeft} steps left)");
            }

            string action = philosopher.GetCurrentActionString();
            if (!string.IsNullOrEmpty(action)) {
                stateBuilder.Append($" (Action = {action})");
            }

            Console.WriteLine($"  {philosopher.Name}: {stateBuilder}, съедено: {philosopher.EatenCount}");
        }

        Console.WriteLine("\nВилки:");
        foreach (var fork in _forks) {
            var stateBuilder = new StringBuilder();
            stateBuilder.Append(fork.State.ToString());

            if (fork.State == ForkState.InUse && fork.UsedByPhilosopher.HasValue) {
                var philosopher = _philosophers[fork.UsedByPhilosopher.Value];
                stateBuilder.Append($" (используется {philosopher.Name})");
            }

            Console.WriteLine($"  Fork-{fork.Id + 1}: {stateBuilder}");
        }
    }
}