using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Arc.OutboundHttpRequestMetrics;

public static class OutboundHttpRequestMetrics
{
    public readonly static Counter HttpRequestCounter = Metrics.CreateCounter("outbound_http_request", help: "Total http requests.");
    public readonly static Counter TimeoutHttpRequestCounter = Metrics.CreateCounter("outbound_timeout_http_request", help: "Total timed out http requests.");
}

public class OutboundHttpRequestObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>
{
    private object _requestLazyInitializerLock = new();
    private object _requestTaskLazyInitializerLock = new();
    private Func<object, HttpRequestMessage> _requestGetterFunction;
    private Func<object, TaskStatus> _requestTaskStatusGetterFunction;
    private bool _requestGetterFunctionIsInitialized;
    private bool _requestTaskGetterFunctionIsInitialized;
    private readonly ILogger<OutboundHttpRequestObserver> _logger;

    public OutboundHttpRequestObserver(ILogger<OutboundHttpRequestObserver> logger)
    {
        this._logger = logger;
    }

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
    {

        if (value.Name == "HttpHandlerDiagnosticListener")
        {
            value.Subscribe(this);
        }
    }

    void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
    {
        bool isStopEvent = value.Key.EndsWith("Stop");
        if (isStopEvent)
        {
            HandleStopEvent(value.Value);
        }
    }

    private void HandleStopEvent(object activityStopData)
    {
        if (activityStopData is null) return;

        HttpRequestMessage httpRequestMessage = GetRequestMessage(activityStopData);
        bool requestMessageHasValue = httpRequestMessage is not null;

        if (requestMessageHasValue)
        {
            bool requestIsCanceled = IsRequestCanceled(activityStopData);
            if (requestIsCanceled is false)
            {
                OutboundHttpRequestMetrics.HttpRequestCounter.Inc(increment: 1);
            }
            else
            {
                OutboundHttpRequestMetrics.TimeoutHttpRequestCounter.Inc(increment: 1);
            }
        }
    }

    private bool IsRequestCanceled(object payload)
    {
        var payloadType = payload.GetType();

        LazyInitializer.EnsureInitialized<Func<object, TaskStatus>>(ref _requestTaskStatusGetterFunction, ref _requestTaskGetterFunctionIsInitialized, ref _requestTaskLazyInitializerLock, () =>
        {
            return GetPropertyGetterFunction<TaskStatus>(payloadType, "RequestTaskStatus");
        });

        TaskStatus taskStatus = _requestTaskStatusGetterFunction(payload);

        return taskStatus == TaskStatus.Canceled ? true : false;
    }

    private HttpRequestMessage GetRequestMessage(object payload)
    {
        var payloadType = payload.GetType();

        LazyInitializer.EnsureInitialized<Func<object, HttpRequestMessage>>(ref _requestGetterFunction, ref _requestGetterFunctionIsInitialized, ref _requestLazyInitializerLock, () =>
        {
            return GetPropertyGetterFunction<HttpRequestMessage>(payloadType, "Request");
        });

        return _requestGetterFunction(payload);
    }

    // https://source.dot.net/#System.Net.Http/System/Net/Http/DiagnosticsHandler.cs,3e6ad991d2a03b5c,references
    private static Func<object, TResult> GetPropertyGetterFunction<TResult>(Type payloadType, string propertyName)
    {
        var objectParameterExpression = Expression.Parameter(typeof(object));
        var typeConversionExpression = Expression.Convert(objectParameterExpression, payloadType);
        var accessPropertyExpression = Expression.Property(typeConversionExpression, propertyName);
        return Expression.Lambda<Func<object, TResult>>(accessPropertyExpression, objectParameterExpression).Compile();
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }
}