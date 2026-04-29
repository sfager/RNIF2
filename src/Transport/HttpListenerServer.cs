using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RNIF2.Core.Configuration;

namespace RNIF2.Transport;

public sealed class HttpListenerServer : IAsyncDisposable
{
    private readonly IRnifRequestPipeline _pipeline;
    private readonly RnifOptions _options;
    private readonly ILogger<HttpListenerServer> _logger;
    private readonly SemaphoreSlim _concurrencyGate;
    private HttpListener? _listener;
    private int _disposed;

    public HttpListenerServer(
        IRnifRequestPipeline pipeline,
        IOptions<RnifOptions> options,
        ILogger<HttpListenerServer> logger)
    {
        _pipeline = pipeline;
        _options = options.Value;
        _logger = logger;
        _concurrencyGate = new SemaphoreSlim(_options.MaxConcurrentRequests);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(_options.ListenerPrefix);
        _listener.Start();
        _logger.LogInformation("RNIF 2.0 server listening on {Prefix}", _options.ListenerPrefix);

        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // I/O operation aborted — normal shutdown on Windows
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting HTTP connection");
                continue;
            }

            await _concurrencyGate.WaitAsync(CancellationToken.None);
            _ = Task.Run(async () =>
            {
                try
                {
                    var requestCtx = HttpRequestContext.From(context);
                    // Use a non-cancellable token for the request itself so we finish in-flight work
                    await _pipeline.HandleAsync(requestCtx, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing RNIF request");
                    try { context.Response.Abort(); } catch { /* best effort */ }
                }
                finally
                {
                    _concurrencyGate.Release();
                }
            }, CancellationToken.None);
        }

        // Drain: wait for all in-flight requests to finish before the caller disposes the listener
        for (var i = 0; i < _options.MaxConcurrentRequests; i++)
            await _concurrencyGate.WaitAsync(CancellationToken.None);

        _logger.LogInformation("RNIF 2.0 server stopped accepting requests");
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return ValueTask.CompletedTask;

        try { _listener?.Stop(); } catch { /* ignore */ }
        try { _listener?.Close(); } catch { /* ignore */ }
        _concurrencyGate.Dispose();
        return ValueTask.CompletedTask;
    }
}
