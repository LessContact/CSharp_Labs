namespace DiningPhilosophers.Contracts.Messages;

public class PhilosopherRegisteredMessage {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

