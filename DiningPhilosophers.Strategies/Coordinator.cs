using DiningPhilosophers.Contracts;

namespace DiningPhilosophers.Strategies;

public class Coordinator : ICoordinator {
    private readonly List<int> _hungryPhilosophers;
    private readonly Dictionary<int, int> _philosopherLeftFork; // какая левая вилка у философа
    private readonly Dictionary<int, int> _philosopherRightFork; // какая правая вилка у философа
    private readonly HashSet<int> _availableForks;

    public event EventHandler<PhilosopherCommandEventArgs>? CommandToPhilosopher;

    public Coordinator(int philosopherCount) {
        _hungryPhilosophers = new List<int>();
        _philosopherLeftFork = new Dictionary<int, int>();
        _philosopherRightFork = new Dictionary<int, int>();
        _availableForks = new HashSet<int>();
        
        for (int i = 0; i < philosopherCount; i++) {
            _availableForks.Add(i);
            _philosopherLeftFork[i] = i;
            _philosopherRightFork[i] = (i + 1) % philosopherCount;
        }
    }

    public void RequestToEat(int philosopherId) {
        _hungryPhilosophers.Add(philosopherId);
    }

    public void NotifyFinishedEating(int philosopherId) {
        _hungryPhilosophers.Remove(philosopherId);
        
        var leftFork = _philosopherLeftFork[philosopherId];
        var rightFork = _philosopherRightFork[philosopherId];
        
        _availableForks.Add(leftFork);
        _availableForks.Add(rightFork);
    }

    public void Step() {
        var philosophersToFeed = new List<int>();

        foreach (var philosopherId in _hungryPhilosophers) {
            var leftFork = _philosopherLeftFork[philosopherId];
            var rightFork = _philosopherRightFork[philosopherId];
            
            if (!_availableForks.Contains(leftFork) || !_availableForks.Contains(rightFork)) continue;

            _availableForks.Remove(leftFork);
            _availableForks.Remove(rightFork);

            philosophersToFeed.Add(philosopherId);
        }
        
        foreach (var philosopherId in philosophersToFeed) {
            _hungryPhilosophers.Remove(philosopherId);
            
            CommandToPhilosopher?.Invoke(this, new PhilosopherCommandEventArgs {
                PhilosopherId = philosopherId,
                Action = PhilosopherAction.TakeLeftFork
            });
        }
    }
}