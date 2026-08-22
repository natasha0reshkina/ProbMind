using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Operations;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/operations")]
[Authorize(Roles = "Admin")]
public sealed class OperationsController : ControllerBase
{
    private readonly ApiMetricsRegistry _metrics;

    public OperationsController(ApiMetricsRegistry metrics) => _metrics = metrics;

    [HttpGet("runtime-metrics")]
    public ApiRuntimeSnapshot RuntimeMetrics([FromQuery] int top = 100) =>
        _metrics.Snapshot(DateTimeOffset.UtcNow, top);

    [HttpGet("process")]
    public object Process()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        return new
        {
            processId = Environment.ProcessId,
            machineName = Environment.MachineName,
            processorCount = Environment.ProcessorCount,
            workingSetBytes = process.WorkingSet64,
            privateMemoryBytes = process.PrivateMemorySize64,
            threads = process.Threads.Count,
            startedAt = process.StartTime.ToUniversalTime(),
            uptimeSeconds = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds,
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        };
    }
}
