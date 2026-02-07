using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fhas.Api.Chat;

public static class OpenRouterChatProxy
{
    public const string HttpClientName = "openrouter";
    public const string ActivitySourceName = "Fhas.Api.Chat";

    private const string DefaultModel = "x-ai/grok-4.1-fast";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static async Task HandleAsync(
        HttpContext httpContext,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Fhas.Api.Chat");

        httpContext.Response.Headers.TryAdd(
            "x-trace-id",
            Activity.Current?.TraceId.ToString() ?? string.Empty);

        ChatCompletionsRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ChatCompletionsRequest>(
                httpContext.Request.Body,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: "Invalid JSON body",
                type: "invalid_request_error",
                code: "invalid_request_error",
                statusCode: StatusCodes.Status400BadRequest,
                cancellationToken);
            return;
        }

        if (body?.Messages is null || body.Messages.Count == 0)
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: "Request must include non-empty \"messages\" array.",
                type: "invalid_request_error",
                code: "invalid_request_error",
                statusCode: StatusCodes.Status400BadRequest,
                cancellationToken);
            return;
        }

        if (config["OPENROUTER_MOCK"] == "1")
        {
            var mock = new
            {
                id = $"chatcmpl-mock-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                @object = "chat.completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = body.Model ?? DefaultModel,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content = "Mocked response." },
                        finish_reason = "stop",
                    },
                },
                usage = new { prompt_tokens = 10, completion_tokens = 3, total_tokens = 13 },
            };

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, mock, JsonOptions, cancellationToken);
            return;
        }

        var apiToken = config["API_TOKEN"];
        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            var auth = httpContext.Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? auth["Bearer ".Length..]
                : null;

            if (!string.Equals(token, apiToken, StringComparison.Ordinal))
            {
                await WriteErrorAsync(
                    httpContext.Response,
                    message: "Invalid or missing API token",
                    type: "invalid_api_key",
                    code: "invalid_api_key",
                    statusCode: StatusCodes.Status401Unauthorized,
                    cancellationToken);
                return;
            }
        }

        var grokKey = config["GROK_KEY"];
        if (string.IsNullOrWhiteSpace(grokKey))
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: "Missing GROK_KEY.",
                type: "server_error",
                code: "server_error",
                statusCode: StatusCodes.Status500InternalServerError,
                cancellationToken);
            return;
        }

        // Custom span makes it easier to locate "agentic workflow" steps in traces.
        using var activity = ActivitySource.StartActivity("openrouter.chat.completions", ActivityKind.Client);
        activity?.SetTag("gen_ai.operation.name", "chat.completions");
        activity?.SetTag("gen_ai.request.model", body.Model ?? DefaultModel);
        activity?.SetTag("gen_ai.system", "openrouter");

        // Build OpenRouter payload (OpenAI-compatible).
        var payload = new Dictionary<string, object?>
        {
            ["model"] = body.Model ?? DefaultModel,
            ["messages"] = body.Messages,
        };
        if (body.Stream is not null) payload["stream"] = body.Stream.Value;
        if (body.Temperature is not null) payload["temperature"] = body.Temperature.Value;
        if (body.MaxTokens is not null) payload["max_tokens"] = body.MaxTokens.Value;

        var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        upstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", grokKey);

        var referer = config["OPENROUTER_REFERER"];
        if (!string.IsNullOrWhiteSpace(referer))
        {
            upstreamRequest.Headers.TryAddWithoutValidation("HTTP-Referer", referer);
        }

        var title = config["OPENROUTER_TITLE"];
        if (!string.IsNullOrWhiteSpace(title))
        {
            upstreamRequest.Headers.TryAddWithoutValidation("X-Title", title);
        }

        upstreamRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            // Streaming mode: pipe OpenRouter response back to the caller (SSE).
            if (body.Stream == true)
            {
                using var upstreamResponse = await client.SendAsync(
                    upstreamRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                httpContext.Response.StatusCode = (int)upstreamResponse.StatusCode;
                httpContext.Response.ContentType =
                    upstreamResponse.Content.Headers.ContentType?.ToString()
                    ?? "text/event-stream";

                await upstreamResponse.Content.CopyToAsync(httpContext.Response.Body, cancellationToken);
                return;
            }

            using var res = await client.SendAsync(upstreamRequest, cancellationToken);
            var text = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                var statusCode =
                    (int)res.StatusCode is >= 400 and < 600
                        ? (int)res.StatusCode
                        : StatusCodes.Status500InternalServerError;

                await WriteErrorAsync(
                    httpContext.Response,
                    message: $"OpenRouter failed: {text ?? res.StatusCode.ToString()}",
                    type: "upstream_error",
                    code: "upstream_error",
                    statusCode: statusCode,
                    cancellationToken);
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(text, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to reach OpenRouter");

            await WriteErrorAsync(
                httpContext.Response,
                message: $"Failed to reach OpenRouter: {ex.Message}",
                type: "network_error",
                code: "network_error",
                statusCode: StatusCodes.Status502BadGateway,
                cancellationToken);
        }
    }

    private static Task WriteErrorAsync(
        HttpResponse response,
        string message,
        string type,
        string code,
        int statusCode,
        CancellationToken cancellationToken)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        response.Headers["Cache-Control"] = "no-store";
        var payload = new ErrorEnvelope(new ErrorBody(message, type, code));
        return JsonSerializer.SerializeAsync(response.Body, payload, JsonOptions, cancellationToken);
    }
}
