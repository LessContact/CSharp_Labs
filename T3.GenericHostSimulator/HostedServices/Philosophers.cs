using DiningPhilosophers.Contracts;
using Microsoft.Extensions.Options;
using T3.GenericHostSimulator.Services;

namespace T3.GenericHostSimulator.HostedServices;

public class AristotleHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(0, "Аристотель", tableManager, metricsCollector, strategy, options);

public class PlatoHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(1, "Платон", tableManager, metricsCollector, strategy, options);

public class SocratesHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(2, "Сократ", tableManager, metricsCollector, strategy, options);

public class DescartesHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(3, "Декарт", tableManager, metricsCollector, strategy, options);

public class KantHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(4, "Кант", tableManager, metricsCollector, strategy, options);
