namespace DiningPhilosophers.Contracts.Messages;

public class TakeForksCommand {
    public string PhilosopherId { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

