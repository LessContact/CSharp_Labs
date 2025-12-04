namespace DatabaseSimulation.Data.Entities;

public class SimulationRun {
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public int PhilosopherCount { get; set; }
    
    public ICollection<PhilosopherStateLog> PhilosopherStateLogs { get; set; } = new List<PhilosopherStateLog>();
    public ICollection<ForkStateLog> ForkStateLogs { get; set; } = new List<ForkStateLog>();
}

