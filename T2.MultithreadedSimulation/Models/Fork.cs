using DiningPhilosophers.Contracts;

namespace T2.MultithreadedSimulation.Models;

public class Fork : IForkInfo {
    public int Id { get; }
    public ForkState State { get; private set; } = ForkState.Available;
    public int? UsedByPhilosopher { get; private set; }

    private readonly Lock _lock = new();
    private long _totalBlockedMs;
    private long _totalEatingMs;
    private DateTime _stateStartTime = DateTime.UtcNow;
    private bool _isEating;

    public Fork(int id) {
        Id = id;
    }

    public bool TryTake(int philosopherId) {
        lock (_lock) {
            if (State is not ForkState.Available) return false;

            RecordElapsedTime();

            State = ForkState.InUse;
            UsedByPhilosopher = philosopherId;
            _isEating = false;
            _stateStartTime = DateTime.UtcNow;
            return true;
        }
    }

    public void Release() {
        lock (_lock) {
            RecordElapsedTime();
            
            State = ForkState.Available;
            UsedByPhilosopher = null;
            _isEating = false;
            _stateStartTime = DateTime.UtcNow;
        }
    }

    public void MarkAsEating() {
        lock (_lock) {
            if (State is not ForkState.InUse || _isEating) return;
            RecordElapsedTime();
            _isEating = true;
            _stateStartTime = DateTime.UtcNow;
        }
    }

    private void RecordElapsedTime() {
        var now = DateTime.UtcNow;
        var elapsed = (long)(now - _stateStartTime).TotalMilliseconds;

        if (State != ForkState.InUse) return;
        if (_isEating) {
            _totalEatingMs += elapsed;
        } else {
            _totalBlockedMs += elapsed;
        }
    }

    public (double available, double blocked, double eating) GetUtilizationPercent(long totalMs) {
        lock (_lock) {
            RecordElapsedTime();
            
            if (totalMs == 0) return (100, 0, 0);

            double blocked = (_totalBlockedMs * 100.0) / totalMs;
            double eating = (_totalEatingMs * 100.0) / totalMs;
            double available = 100.0 - blocked - eating;

            return (available, blocked, eating);
        }
    }
}

