using DiningPhilosophers.Contracts;

namespace TableService.Models;

/// <summary>
/// Информация о зарегистрированном философе
/// </summary>
public class PhilosopherInfo {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public PhilosopherState State { get; set; } = PhilosopherState.Thinking;
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
    public long TotalWaitingMs { get; set; }
    public bool HasExited { get; set; }

    public PhilosopherStatusInfo ToStatusInfo() {
        return new PhilosopherStatusInfo {
            PhilosopherId = PhilosopherId,
            PhilosopherName = PhilosopherName,
            State = State,
            HasLeftFork = HasLeftFork,
            HasRightFork = HasRightFork,
            EatenCount = EatenCount,
            TotalWaitingMs = TotalWaitingMs
        };
    }
}

