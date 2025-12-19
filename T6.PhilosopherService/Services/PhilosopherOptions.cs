namespace PhilosopherService.Services;
 
public class PhilosopherOptions {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public int SimulationDurationMinutes { get; set; } = 5;
    public int ThinkingTimeMinMs { get; set; } = 100;
    public int ThinkingTimeMaxMs { get; set; } = 500;
    public int EatingTimeMinMs { get; set; } = 100;
    public int EatingTimeMaxMs { get; set; } = 300;
    public int ForkAcquisitionTimeMs { get; set; } = 50;
    public int RetryDelayMs { get; set; } = 10;
    public string StrategyName { get; set; } = "Hierarchy";
}

