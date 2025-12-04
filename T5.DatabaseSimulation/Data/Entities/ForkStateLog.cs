using DiningPhilosophers.Contracts;

namespace DatabaseSimulation.Data.Entities;

public class ForkStateLog {
    public long Id { get; set; }
    public int SimulationRunId { get; set; }
    public SimulationRun SimulationRun { get; set; } = null!;
    
    public int ForkId { get; set; }

    // Время в миллисекундах от начала симуляции
    public long TimestampMs { get; set; }
    
    public ForkState State { get; set; }
    public int? UsedByPhilosopherId { get; set; }
    public string? UsedByPhilosopherName { get; set; }
}

