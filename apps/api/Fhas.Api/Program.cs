using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Fhas.Api.Chat;
using Fhas.Agent.Runtime;
using Fhas.Agent.Skills;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "fhas-api";
var serviceVersion =
    typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Default: allow any origin in dev; lock this down for prod.
        var corsOrigin = builder.Configuration["CORS_ORIGIN"];
        if (!string.IsNullOrWhiteSpace(corsOrigin) && corsOrigin != "*")
        {
            policy.WithOrigins(corsOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
    {
        // Match the template server behavior: 100 req/min per client IP.
        var partitionKey =
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

// "Skills" scaffolding (inspired by health-skillz). This stays intentionally minimal for now.
builder.Services.AddSingleton<ISkill, EchoSkill>();
builder.Services.AddSingleton<SkillRegistry>();
builder.Services.AddSingleton<SkillToolCatalog>();

// Typed HttpClient for OpenRouter-backed agent execution.
builder.Services.AddHttpClient<OpenRouterAgentRunner>((sp, client) =>
{
    client.BaseAddress = OpenRouterAgentRunner.OpenRouterEndpoint;
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var cfg = sp.GetRequiredService<IConfiguration>();

    var referer = cfg["OPENROUTER_REFERER"];
    if (!string.IsNullOrWhiteSpace(referer))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", referer);
    }

    var title = cfg["OPENROUTER_TITLE"];
    if (!string.IsNullOrWhiteSpace(title))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", title);
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OpenTelemetry (traces + metrics + logs).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(ChatCompletionsEndpoint.ActivitySourceName)
            .AddSource(OpenRouterAgentRunner.ActivitySourceName);

        var hasOtlpTraces =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"));

        if (hasOtlpTraces)
        {
            tracing.AddOtlpExporter();
        }
        else if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT")))
        {
            metrics.AddOtlpExporter();
        }
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;

    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT")))
    {
        options.AddOtlpExporter();
    }
});

var app = builder.Build();

var startedAt = Stopwatch.StartNew();

app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/healthz", () => Results.Text("ok"))
    .ExcludeFromDescription();

var api = app.MapGroup("/api")
    .RequireRateLimiting("api");

var v1 = api.MapGroup("/v1");

var chat = v1.MapGroup("/chat");
chat.MapGet("/health", () =>
{
    var res = new HealthResponse(
        Status: "healthy",
        UptimeMs: startedAt.ElapsedMilliseconds,
        Timestamp: DateTimeOffset.UtcNow.ToString("O"),
        Version: serviceVersion);
    return Results.Json(res);
});

chat.MapPost("/completions", ChatCompletionsEndpoint.HandleAsync);

var skills = v1.MapGroup("/skills");
skills.MapGet("", (SkillRegistry registry) => Results.Json(registry.List()));

app.Run();

public partial class Program { }
