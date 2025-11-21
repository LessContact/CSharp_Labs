using T3.GenericHostSimulator.Models;

namespace T3.GenericHostSimulator.Services;

public interface ITableManager {
    Fork GetLeftFork(int philosopherId);
    Fork GetRightFork(int philosopherId);
    IReadOnlyList<Fork> GetAllForks();
}

public class TableManager : ITableManager {
    private readonly List<Fork> _forks;
    private readonly int _philosopherCount;

    public TableManager(int philosopherCount) {
        _philosopherCount = philosopherCount;
        _forks = new List<Fork>();
        for (var i = 0; i < philosopherCount; i++) _forks.Add(new Fork(i));
    }

    public Fork GetLeftFork(int philosopherId) {
        return _forks[philosopherId];
    }

    public Fork GetRightFork(int philosopherId) {
        return _forks[(philosopherId + 1) % _philosopherCount];
    }

    public IReadOnlyList<Fork> GetAllForks() {
        return _forks.AsReadOnly();
    }
}