using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fhas.Agent.Runtime;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using System.ClientModel;

namespace Fhas.Api.Chat;

public static class ChatCompletionsEndpoint
{
    public const string ActivitySourceName = "Fhas.Api.Chat";

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
        OpenRouterAgentRunner runner,
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

        if (body.Stream == true)
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: "stream=true is not supported in this baseline.",
                type: "invalid_request_error",
                code: "stream_not_supported",
                statusCode: StatusCodes.Status400BadRequest,
                cancellationToken);
            return;
        }

        var model = body.Model ?? OpenRouterAgentRunner.DefaultModel;
        if (!IsModelAllowed(config["MODEL_ALLOWLIST"], model))
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: $"Model \"{model}\" is not allowed.",
                type: "invalid_request_error",
                code: "model_not_allowed",
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
                model,
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

        var apiKey = config["OPENROUTER_API_KEY"] ?? config["GROK_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: "Missing OPENROUTER_API_KEY (or GROK_KEY for backward compatibility).",
                type: "server_error",
                code: "server_error",
                statusCode: StatusCodes.Status500InternalServerError,
                cancellationToken);
            return;
        }

        List<Microsoft.Extensions.AI.ChatMessage> agentMessages;
        try
        {
            agentMessages = body.Messages
                .Select(ToAgentMessage)
                .ToList();
        }
        catch (ArgumentException ex)
        {
            await WriteErrorAsync(
                httpContext.Response,
                message: ex.Message,
                type: "invalid_request_error",
                code: "invalid_request_error",
                statusCode: StatusCodes.Status400BadRequest,
                cancellationToken);
            return;
        }

        // Custom span makes it easier to locate "agentic workflow" steps in traces.
        using var activity = ActivitySource.StartActivity("agent.chat.completions", ActivityKind.Internal);
        activity?.SetTag("gen_ai.operation.name", "chat.completions");
        activity?.SetTag("gen_ai.request.model", model);
        activity?.SetTag("gen_ai.system", "openrouter");

        AgentResponse agentResponse;
        try
        {
            agentResponse = await runner.RunAsync(
                agentMessages,
                new OpenRouterAgentRunOptions(
                    ApiKey: apiKey,
                    Model: model,
                    Temperature: body.Temperature,
                    MaxOutputTokens: body.MaxTokens,
                    Instructions: null),
                cancellationToken);
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
            return;
        }
        catch (ClientResultException ex)
        {
            var status = ex.Status is >= 400 and < 600 ? ex.Status : StatusCodes.Status502BadGateway;
            logger.LogWarning(ex, "OpenRouter returned a non-success response");
            await WriteErrorAsync(
                httpContext.Response,
                message: ex.Message,
                type: "upstream_error",
                code: "upstream_error",
                statusCode: status,
                cancellationToken);
            return;
        }

        var assistantText = ExtractAssistantText(agentResponse);
        var usage = agentResponse.Usage;

        var response = new
        {
            id = agentResponse.ResponseId ?? $"chatcmpl-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            @object = "chat.completion",
            created = (agentResponse.CreatedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds(),
            model,
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new { role = "assistant", content = assistantText },
                    finish_reason = "stop",
                },
            },
            usage = new
            {
                prompt_tokens = usage?.InputTokenCount ?? 0,
                completion_tokens = usage?.OutputTokenCount ?? 0,
                total_tokens = usage?.TotalTokenCount ?? 0,
            },
        };

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, response, JsonOptions, cancellationToken);
    }

    private static Microsoft.Extensions.AI.ChatMessage ToAgentMessage(ChatMessage msg)
    {
        var role = msg.Role.Trim();
        return role.ToLowerInvariant() switch
        {
            "user" => new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, msg.Content),
            "assistant" => new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, msg.Content),
            "system" => new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, msg.Content),
            _ => throw new ArgumentException($"Unsupported role \"{msg.Role}\". Allowed: user|assistant|system."),
        };
    }

    private static string ExtractAssistantText(AgentResponse response)
    {
        // Prefer the last assistant message. Fall back to concatenated text if needed.
        var lastAssistant = response.Messages
            .LastOrDefault(m => m.Role == ChatRole.Assistant);
        return lastAssistant?.Text ?? response.Text ?? string.Empty;
    }

    private static bool IsModelAllowed(string? allowlistRaw, string model)
    {
        if (string.IsNullOrWhiteSpace(allowlistRaw))
        {
            return true;
        }

        var allowed = allowlistRaw.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return allowed.Contains(model, StringComparer.Ordinal);
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
