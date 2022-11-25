using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Arc.OutboundHttpRequestMetrics;

internal sealed class DiagnosticsHostedService : IHostedService
{
    private readonly OutboundHttpRequestObserver _observer;
    private IDisposable _subscription;

    public DiagnosticsHostedService(OutboundHttpRequestObserver observer)
    {
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription ??= DiagnosticListener.AllListeners.Subscribe(_observer);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        return Task.CompletedTask;
    }
}