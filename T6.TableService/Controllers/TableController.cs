using Microsoft.AspNetCore.Mvc;
using TableService.Services;
using DiningPhilosophers.Contracts;

namespace TableService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TableController : ControllerBase {
    private readonly ITableManager _tableManager;
    private readonly ILogger<TableController> _logger;

    public TableController(ITableManager tableManager, ILogger<TableController> logger) {
        _tableManager = tableManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public ActionResult<RegisterPhilosopherResponse> RegisterPhilosopher([FromBody] RegisterPhilosopherRequest request) {
        var success = _tableManager.RegisterPhilosopher(
            request.PhilosopherId,
            request.PhilosopherName,
            request.LeftForkId,
            request.RightForkId);

        return Ok(new RegisterPhilosopherResponse {
            Success = success,
            Message = success ? "Registered successfully" : "Already registered"
        });
    }

    [HttpPost("fork/take")]
    public ActionResult<TakeForkResponse> TakeFork([FromBody] TakeForkRequest request) {
        try {
            var fork = _tableManager.GetFork(request.ForkId);
            var success = fork.TryTake(request.PhilosopherId, request.PhilosopherName);

            return Ok(new TakeForkResponse {
                Success = success,
                ForkId = request.ForkId,
                State = fork.ToForkInfo().State,
                HeldByPhilosopher = fork.ToForkInfo().HeldByPhilosopher
            });
        }
        catch (ArgumentOutOfRangeException) {
            return BadRequest(new TakeForkResponse {
                Success = false,
                ForkId = request.ForkId
            });
        }
    }

    [HttpPost("fork/release")]
    public ActionResult<ReleaseForkResponse> ReleaseFork([FromBody] ReleaseForkRequest request) {
        try {
            var fork = _tableManager.GetFork(request.ForkId);
            var success = fork.Release(request.PhilosopherId);

            return Ok(new ReleaseForkResponse {
                Success = success,
                ForkId = request.ForkId
            });
        }
        catch (ArgumentOutOfRangeException) {
            return BadRequest(new ReleaseForkResponse {
                Success = false,
                ForkId = request.ForkId
            });
        }
    }

    [HttpGet("fork/{forkId:int}")]
    public ActionResult<ForkInfo> GetForkState(int forkId) {
        try {
            var fork = _tableManager.GetFork(forkId);
            return Ok(fork.ToForkInfo());
        }
        catch (ArgumentOutOfRangeException) {
            return NotFound();
        }
    }
    
    [HttpPost("eating/start")]
    public IActionResult StartEating([FromBody] StartEatingRequest request) {
        try {
            var leftFork = _tableManager.GetFork(request.LeftForkId);
            var rightFork = _tableManager.GetFork(request.RightForkId);

            leftFork.MarkAsEating();
            rightFork.MarkAsEating();

            return Ok();
        }
        catch (ArgumentOutOfRangeException) {
            return BadRequest();
        }
    }
    
    [HttpPost("meal/record")]
    public IActionResult RecordMeal([FromBody] RecordMealRequest request) {
        _tableManager.RecordMeal(request.PhilosopherId, request.PhilosopherName, request.WaitingTimeMs);
        return Ok();
    }
    
    [HttpPost("philosopher/state")]
    public IActionResult UpdatePhilosopherState([FromBody] UpdatePhilosopherStateRequest request) {
        _tableManager.UpdatePhilosopherState(
            request.PhilosopherId,
            request.State,
            request.HasLeftFork,
            request.HasRightFork,
            request.EatenCount);
        return Ok();
    }
    
    [HttpPost("philosopher/exit")]
    public IActionResult PhilosopherExit([FromBody] PhilosopherExitRequest request) {
        _tableManager.PhilosopherExit(request.PhilosopherId, request.PhilosopherName, request.TotalMeals);
        return Ok();
    }
    
    [HttpGet("status")]
    public ActionResult<SimulationStatusResponse> GetStatus() {
        return Ok(_tableManager.GetStatus());
    }
    
    [HttpGet("metrics")]
    public ActionResult<SimulationMetrics> GetMetrics() {
        return Ok(_tableManager.GetFinalMetrics());
    }
}

