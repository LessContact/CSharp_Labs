namespace DiningPhilosophers.Contracts;

public class TakeForkRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int ForkId { get; set; }
}

public class TakeForkResponse {
    public bool Success { get; set; }
    public int ForkId { get; set; }
    public ForkState State { get; set; }
    public string? HeldByPhilosopher { get; set; }
}

public class ReleaseForkRequest {
    public string PhilosopherId { get; set; } = "";
    public int ForkId { get; set; }
}

public class ReleaseForkResponse {
    public bool Success { get; set; }
    public int ForkId { get; set; }
}

public class ForkInfo : IForkInfo {
    public int Id { get; set; }
    public ForkState State { get; set; }
    public string? HeldByPhilosopher { get; set; }
}

public class StartEatingRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
}

public class RecordMealRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public long WaitingTimeMs { get; set; }
}

public class PhilosopherExitRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int TotalMeals { get; set; }
}

public class RegisterPhilosopherRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
}

public class RegisterPhilosopherResponse {
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class PhilosopherStatusInfo {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public PhilosopherState State { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
    public long TotalWaitingMs { get; set; }
}

public class UpdatePhilosopherStateRequest {
    public string PhilosopherId { get; set; } = "";
    public PhilosopherState State { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
}

public class SimulationStatusResponse {
    public List<PhilosopherStatusInfo> Philosophers { get; set; } = new();
    public List<ForkInfo> Forks { get; set; } = new();
    public long ElapsedMs { get; set; }
    public bool IsRunning { get; set; }
}

public class SimulationMetrics {
    public int TotalMeals { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageThroughput { get; set; }
    public double AverageWaitingTimeMs { get; set; }
    public long MaxWaitingTimeMs { get; set; }
    public string? MaxWaitingPhilosopher { get; set; }
    public List<PhilosopherMetricInfo> PhilosopherMetrics { get; set; } = new();
    public List<ForkUtilizationInfo> ForkUtilizations { get; set; } = new();
}

public class PhilosopherMetricInfo {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int EatenCount { get; set; }
    public double Throughput { get; set; }
    public long TotalWaitingMs { get; set; }
    public double AverageWaitingMs { get; set; }
}

public class ForkUtilizationInfo {
    public int ForkId { get; set; }
    public double AvailablePercent { get; set; }
    public double BlockedPercent { get; set; }
    public double EatingPercent { get; set; }
}