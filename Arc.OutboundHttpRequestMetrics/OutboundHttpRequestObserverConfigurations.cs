using Microsoft.Extensions.DependencyInjection;

namespace Arc.OutboundHttpRequestMetrics;

public static class OutboundHttpRequestObserverConfigurations
{
    public static void AddOutboundHttpRequestMetrics(this IServiceCollection services)
    {
        services.AddSingleton<OutboundHttpRequestObserver>();
        services.AddHostedService<DiagnosticsHostedService>();
    }
}