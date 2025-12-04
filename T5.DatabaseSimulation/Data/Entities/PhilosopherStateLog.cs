using DiningPhilosophers.Contracts;

namespace DatabaseSimulation.Data.Entities;

public class PhilosopherStateLog {
    public long Id { get; set; }
    public int SimulationRunId { get; set; }
    public SimulationRun SimulationRun { get; set; } = null!;
    
    public int PhilosopherId { get; set; }
    public string PhilosopherName { get; set; } = string.Empty;
    
    // Время в миллисекундах от начала симуляции
    public long TimestampMs { get; set; }
    
    public PhilosopherState State { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
    public PhilosopherAction? Action { get; set; }
}

