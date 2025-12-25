namespace DiningPhilosophers.Contracts.Messages;

public class HungryMessage {
    public string PhilosopherId { set; get; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
