using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace Fhas.Agent.Runtime;

public sealed record OpenRouterAgentRunOptions(
    string ApiKey,
    string Model,
    double? Temperature,
    int? MaxOutputTokens,
    string? Instructions
);

public sealed class OpenRouterAgentRunner
{
    public const string DefaultModel = "x-ai/grok-4.1-fast";
    public static readonly Uri OpenRouterEndpoint = new("https://openrouter.ai/api/v1/");

    public const string ActivitySourceName = "Fhas.Agent.Runtime";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly HttpClient _httpClient;
    private readonly SkillToolCatalog _toolCatalog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _services;

    public OpenRouterAgentRunner(
        HttpClient httpClient,
        SkillToolCatalog toolCatalog,
        ILoggerFactory loggerFactory,
        IServiceProvider services)
    {
        _httpClient = httpClient;
        _toolCatalog = toolCatalog;
        _loggerFactory = loggerFactory;
        _services = services;
    }

    public async Task<AgentResponse> RunAsync(
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> messages,
        OpenRouterAgentRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(options);

        using var activity = ActivitySource.StartActivity("agent.run", ActivityKind.Internal);
        activity?.SetTag("gen_ai.system", "openrouter");
        activity?.SetTag("gen_ai.request.model", options.Model);

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = OpenRouterEndpoint,
            UserAgentApplicationId = "fhas-api",

            // Use the preconfigured HttpClient so we can share handler settings and optionally include extra headers.
            Transport = new HttpClientPipelineTransport(_httpClient),
        };

        var openAi = new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);
        var chatClient = openAi.GetChatClient(options.Model);

        var instructions =
            options.Instructions ??
            "You are the FHAS assistant. Use available tools when they help. Keep responses concise and avoid PHI.";

        var agent = chatClient.AsAIAgent(
            instructions: instructions,
            name: "fhas",
            description: "FHAS agent (internal skills + OpenRouter).",
            tools: _toolCatalog.Tools,
            clientFactory: null,
            loggerFactory: _loggerFactory,
            services: _services);

        var runChatOptions = new ChatOptions
        {
            ModelId = options.Model,
            Temperature = options.Temperature is null ? null : (float)options.Temperature.Value,
            MaxOutputTokens = options.MaxOutputTokens,
        };

        // Note: tools were supplied at agent creation; run options are still used for per-request params.
        var runOptions = new ChatClientAgentRunOptions(runChatOptions);

        return await agent.RunAsync(messages, session: null, options: runOptions, cancellationToken);
    }
}
