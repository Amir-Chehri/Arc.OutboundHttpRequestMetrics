using Arc.OutboundHttpRequestMetrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostingContext, services) =>
    {
        services.AddHttpClient();
        services.AddOutboundHttpRequestMetrics();
        services.AddHostedService<TestBackgroundService>();
    }).Build();

var metricServer = new MetricServer(5000, "metrics/", useHttps: false);
metricServer.Start();

await host.RunAsync();

public class TestBackgroundService : BackgroundService
{
    private readonly HttpClient _client;

    public TestBackgroundService(HttpClient client)
    {
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HttpResponseMessage response = await _client.GetAsync("https://google.com");
    }
}