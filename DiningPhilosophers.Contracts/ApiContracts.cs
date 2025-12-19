using DiningPhilosophers.Contracts;

/// <summary>
/// Запрос на попытку взять вилку
/// </summary>
public class TakeForkRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int ForkId { get; set; }
}

/// <summary>
/// Ответ на запрос взятия вилки
/// </summary>
public class TakeForkResponse {
    public bool Success { get; set; }
    public int ForkId { get; set; }
    public ForkState State { get; set; }
    public string? HeldByPhilosopher { get; set; }
}

/// <summary>
/// Запрос на освобождение вилки
/// </summary>
public class ReleaseForkRequest {
    public string PhilosopherId { get; set; } = "";
    public int ForkId { get; set; }
}

/// <summary>
/// Ответ на освобождение вилки
/// </summary>
public class ReleaseForkResponse {
    public bool Success { get; set; }
    public int ForkId { get; set; }
}

/// <summary>
/// Информация о состоянии вилки
/// </summary>
public class ForkInfo : IForkInfo {
    public int Id { get; set; }
    public ForkState State { get; set; }
    public string? HeldByPhilosopher { get; set; }
}

/// <summary>
/// Запрос на получение состояния вилки
/// </summary>
public class GetForkStateRequest {
    public int ForkId { get; set; }
}

/// <summary>
/// Уведомление о начале приёма пищи
/// </summary>
public class StartEatingRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
}

/// <summary>
/// Уведомление о завершении приёма пищи (запись метрики)
/// </summary>
public class RecordMealRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public long WaitingTimeMs { get; set; }
}

/// <summary>
/// Уведомление о выходе философа из симуляции
/// </summary>
public class PhilosopherExitRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int TotalMeals { get; set; }
}

/// <summary>
/// Запрос регистрации философа в системе
/// </summary>
public class RegisterPhilosopherRequest {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
}

/// <summary>
/// Ответ на регистрацию философа
/// </summary>
public class RegisterPhilosopherResponse {
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Состояние философа для отображения
/// </summary>
public class PhilosopherStatusInfo {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public PhilosopherState State { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
    public long TotalWaitingMs { get; set; }
}

/// <summary>
/// Обновление состояния философа на столе
/// </summary>
public class UpdatePhilosopherStateRequest {
    public string PhilosopherId { get; set; } = "";
    public PhilosopherState State { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public int EatenCount { get; set; }
}

/// <summary>
/// Информация о полном состоянии симуляции
/// </summary>
public class SimulationStatusResponse {
    public List<PhilosopherStatusInfo> Philosophers { get; set; } = new();
    public List<ForkInfo> Forks { get; set; } = new();
    public long ElapsedMs { get; set; }
    public bool IsRunning { get; set; }
}

/// <summary>
/// Метрики симуляции
/// </summary>
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

/// <summary>
/// Метрики отдельного философа
/// </summary>
public class PhilosopherMetricInfo {
    public string PhilosopherId { get; set; } = "";
    public string PhilosopherName { get; set; } = "";
    public int EatenCount { get; set; }
    public double Throughput { get; set; }
    public long TotalWaitingMs { get; set; }
    public double AverageWaitingMs { get; set; }
}

/// <summary>
/// Утилизация вилки
/// </summary>
public class ForkUtilizationInfo {
    public int ForkId { get; set; }
    public double AvailablePercent { get; set; }
    public double BlockedPercent { get; set; }
    public double EatingPercent { get; set; }
}

