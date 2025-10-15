using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração dos Serviços Base da Aplicação ---
builder.Services.AddControllers();
builder.Services.AddHealthChecks(); // Adiciona o serviço de health checks

// ActivitySource para spans manuais, se necessário
var activitySource = new ActivitySource("TestMetricsService.Manual");


// --- 2. Configuração Unificada do OpenTelemetry ---

// Define o nome do serviço e outros recursos uma única vez
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("TestMetricsService", serviceVersion: "1.0.0");

// Endereço do OpenTelemetry Collector (usando o nome de serviço DNS completo do Kubernetes)
var otelCollectorEndpoint = "[http://otel-collector-service.monitoring.svc.cluster.local:4318](http://otel-collector-service.monitoring.svc.cluster.local:4318)";

// Configuração do Logging para enviar para o OTel Collector
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri(otelCollectorEndpoint);
        o.Protocol = OtlpExportProtocol.HttpProtobuf; // Usar HTTP/protobuf é geralmente mais simples
    });
});

// Configuração principal do OpenTelemetry para Métricas e Traces
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resourceBuilder)
        .AddSource("TestMetricsService.Manual") // Para spans manuais
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(otelCollectorEndpoint);
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(otelCollectorEndpoint);
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));


// --- 3. Construção e Mapeamento dos Endpoints da Aplicação ---
var app = builder.Build();

// Endpoint para o Prometheus fazer scrape diretamente
app.MapMetrics();

// Endpoint de Health Check para o Kubernetes
app.MapHealthChecks("/health");

// Endpoint para a telemetria do seu dashboard Grafana
app.MapGet("/hello", (ILogger<Program> logger) =>
{
    using var span = activitySource.StartActivity("hello-work", ActivityKind.Server);
    span?.SetTag("custom.attr", "ping");

    logger.LogInformation("Endpoint /hello chamado em {ts}", DateTimeOffset.Now);
    return Results.Ok(new { Message = "Olá do micro-serviço!" });
});

app.Run();
    

