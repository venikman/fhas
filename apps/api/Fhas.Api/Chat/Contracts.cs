using System.Text.Json.Serialization;

namespace Fhas.Api.Chat;

public sealed record ChatCompletionsRequest(
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage>? Messages,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("stream")] bool? Stream,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens
);

public sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public sealed record HealthResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("uptime_ms")] long UptimeMs,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("version")] string Version
);

public sealed record ErrorEnvelope(
    [property: JsonPropertyName("error")] ErrorBody Error
);

public sealed record ErrorBody(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("code")] string Code
);

