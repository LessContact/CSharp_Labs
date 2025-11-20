using DiningPhilosophers.Contracts;

namespace T2.MultithreadedSimulation.Models;

public class Philosopher {
    public int Id { get; }
    public string Name { get; }
    public PhilosopherState State { get; private set; }
    public int EatenCount { get; private set; }
    public long TotalWaitingMs { get; private set; }

    private readonly Fork _leftFork;
    private readonly Fork _rightFork;
    private readonly IStrategy _strategy;
    private readonly Random _random;
    private readonly CancellationToken _cancellationToken;

    private bool _hasLeftFork;
    private bool _hasRightFork;
    private DateTime _hungryStartTime;
    private DateTime _eatingStartTime;

    public Philosopher(int id, string name, Fork leftFork, Fork rightFork, 
        IStrategy strategy, Random random, CancellationToken cancellationToken) {
        Id = id;
        Name = name;
        _leftFork = leftFork;
        _rightFork = rightFork;
        _strategy = strategy;
        _random = random;
        _cancellationToken = cancellationToken;
        State = PhilosopherState.Thinking;
    }

    public async Task RunAsync() {
        while (!_cancellationToken.IsCancellationRequested) {
            switch (State) {
                case PhilosopherState.Thinking:
                    await ThinkAsync();
                    break;
                case PhilosopherState.Hungry:
                    await TryToEatAsync();
                    break;
                case PhilosopherState.Eating:
                    await EatAsync();
                    break;
            }
        }

        ReleaseForks();
    }

    private async Task ThinkAsync() {
        int thinkingTime = _random.Next(30, 101); // 30-100 мс
        await Task.Delay(thinkingTime, _cancellationToken);

        State = PhilosopherState.Hungry;
        _hungryStartTime = DateTime.UtcNow;
    }

    private async Task TryToEatAsync() {
        var action = _strategy.DecideAction(_leftFork, _rightFork, State, _hasLeftFork, _hasRightFork);

        switch (action) {
            case PhilosopherAction.TakeLeftFork:
                if (!_hasLeftFork) {
                    await Task.Delay(20, _cancellationToken); // 20 мс на взятие вилки
                    if (_leftFork.TryTake(Id)) {
                        _hasLeftFork = true;
                    }
                }
                break;

            case PhilosopherAction.TakeRightFork:
                if (!_hasRightFork) {
                    await Task.Delay(20, _cancellationToken); // 20 мс на взятие вилки
                    if (_rightFork.TryTake(Id)) {
                        _hasRightFork = true;
                    }
                }
                break;

            case PhilosopherAction.ReleaseLeftFork:
                if (_hasLeftFork) {
                    _leftFork.Release();
                    _hasLeftFork = false;
                }
                break;

            case PhilosopherAction.ReleaseRightFork:
                if (_hasRightFork) {
                    _rightFork.Release();
                    _hasRightFork = false;
                }
                break;

            case PhilosopherAction.ReleaseBothForks:
                ReleaseForks();
                break;

            case PhilosopherAction.None:
                break;
        }
        
        if (_hasLeftFork && _hasRightFork) {
            State = PhilosopherState.Eating;
            _eatingStartTime = DateTime.UtcNow;
            TotalWaitingMs += (long)(_eatingStartTime - _hungryStartTime).TotalMilliseconds;
            
            _leftFork.MarkAsEating();
            _rightFork.MarkAsEating();
        }
    }

    private async Task EatAsync() {
        int eatingTime = _random.Next(40, 51); // 40-50 мс
        
        await Task.Delay(eatingTime, _cancellationToken);

        EatenCount++;
        
        ReleaseForks();
        
        State = PhilosopherState.Thinking;
    }

    private void ReleaseForks() {
        if (_hasLeftFork) {
            _leftFork.Release();
            _hasLeftFork = false;
        }

        if (_hasRightFork) {
            _rightFork.Release();
            _hasRightFork = false;
        }
    }
    
    public bool HasLeftFork => _hasLeftFork;
    
    public bool HasRightFork => _hasRightFork;
}
