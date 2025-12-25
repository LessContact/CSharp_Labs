namespace DiningPhilosophers.Contracts.Messages;

public class PhilosopherExitedMessage {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int TotalMeals { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

