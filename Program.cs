using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using Prometheus;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// TEMP activity source for a manual span
var activitySource = new ActivitySource("TestMetricsService.Manual");

// >>> switch to gRPC (internal Docker port 4317)
var otlpGrpc = new Uri("http://otel-collector:4317");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("TestMetricsService"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddSource("TestMetricsService.Manual");
        t.AddOtlpExporter(o =>
        {
            o.Protocol = OtlpExportProtocol.Grpc;
            o.Endpoint = otlpGrpc;
        });
        t.AddConsoleExporter(); // TEMP confirm
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddRuntimeInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddProcessInstrumentation();
        m.AddOtlpExporter(o =>
        {
            o.Protocol = OtlpExportProtocol.Grpc;
            o.Endpoint = otlpGrpc;
        });
        m.AddConsoleExporter(); // TEMP confirm
    });

builder.Logging.AddOpenTelemetry(o =>
    {
        o.IncludeFormattedMessage = true;
        o.IncludeScopes = true;
        o.ParseStateValues = true;
        o.AddOtlpExporter(ol =>
        {
            ol.Protocol = OtlpExportProtocol.Grpc;
            ol.Endpoint = otlpGrpc;
        });
    });

// ----- Logging: use OpenTelemetry provider (temporarily drop Serilog) -----
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddOpenTelemetry(o =>
    {
        o.IncludeFormattedMessage = true;
        o.IncludeScopes = true;
        o.ParseStateValues = true;
        o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TestMetricsService")); // <-- add this
        o.AddOtlpExporter(ol =>
        {
            ol.Endpoint = new Uri("http://otel-collector:4318");
            ol.Protocol = OtlpExportProtocol.HttpProtobuf;
        });
    });

    // ----- OpenTelemetry: Traces + Metrics -----
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("TestMetricsService"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri("http://otel-collector:4318");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            }))
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri("http://otel-collector:4318");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            }));


    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // prometheus-net scrape (direct Prometheus path)
    app.UseHttpMetrics();
    app.MapMetrics();

    app.MapGet("/hello", (ILogger<Program> logger) =>
    {
        using var span = activitySource.StartActivity("hello-work", ActivityKind.Server);
        span?.SetTag("custom.attr", "ping");

        logger.LogInformation("Hello endpoint called at {ts}", DateTimeOffset.Now);
        return Results.Ok(new { Message = "Hello from microservice!" });
    });
    
    app.MapHealthChecks("/health");
    
    Console.WriteLine("Hello from the automated pipeline!");

app.Run();
