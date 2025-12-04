using DatabaseSimulation.Services;
using DiningPhilosophers.Contracts;
using Microsoft.Extensions.Options;

namespace DatabaseSimulation.HostedServices;

public class AristotleHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStateLogger stateLogger,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(0, "Аристотель", tableManager, metricsCollector, stateLogger, strategy, options);

public class PlatoHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStateLogger stateLogger,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(1, "Платон", tableManager, metricsCollector, stateLogger, strategy, options);

public class SocratesHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStateLogger stateLogger,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(2, "Сократ", tableManager, metricsCollector, stateLogger, strategy, options);

public class DescartesHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStateLogger stateLogger,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(3, "Декарт", tableManager, metricsCollector, stateLogger, strategy, options);

public class KantHostedService(
    ITableManager tableManager,
    IMetricsCollector metricsCollector,
    IStateLogger stateLogger,
    IStrategy strategy,
    IOptions<SimulationOptions> options)
    : PhilosopherHostedService(4, "Кант", tableManager, metricsCollector, stateLogger, strategy, options);

