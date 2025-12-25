namespace T7.CoordinatorService.Services;

public class CoordinatorOptions {
    public int PhilosophersCount { get; set; } = 5;
    public string RabbitMqHost { get; set; } = "localhost";
    public int CoordinatorStepIntervalMs { get; set; } = 10;
}
