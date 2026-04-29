using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RNIF2.Transport;

namespace RNIF2.Host;

public sealed class Worker : BackgroundService
{
    private readonly HttpListenerServer _server;
    private readonly ILogger<Worker> _logger;

    public Worker(HttpListenerServer server, ILogger<Worker> logger)
    {
        _server = server;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RNIF 2.0 Worker Service starting");
        await _server.StartAsync(stoppingToken);
        _logger.LogInformation("RNIF 2.0 Worker Service stopped");
    }
}
