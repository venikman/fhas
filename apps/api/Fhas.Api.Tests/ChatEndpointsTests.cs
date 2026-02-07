using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Fhas.Api.Tests;

public sealed class ChatEndpointsTests
{
    [Fact]
    public async Task Completions_Returns_OpenAI_Format_When_Mocked()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["OPENROUTER_MOCK"] = "1",
        });

        using var client = factory.CreateClient();

        using var res = await client.PostAsJsonAsync(
            "/api/v1/chat/completions",
            new
            {
                messages = new[]
                {
                    new { role = "user", content = "Hello" },
                },
            });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.StartsWith("chatcmpl-", root.GetProperty("id").GetString());
        Assert.Equal("chat.completion", root.GetProperty("object").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("model").GetString()));

        var choices = root.GetProperty("choices");
        Assert.Equal(JsonValueKind.Array, choices.ValueKind);
        Assert.Equal(1, choices.GetArrayLength());

        var choice0 = choices[0];
        Assert.Equal("assistant", choice0.GetProperty("message").GetProperty("role").GetString());
        Assert.Equal("Mocked response.", choice0.GetProperty("message").GetProperty("content").GetString());
        Assert.Equal("stop", choice0.GetProperty("finish_reason").GetString());

        var usage = root.GetProperty("usage");
        Assert.True(usage.GetProperty("prompt_tokens").GetInt32() >= 0);
        Assert.True(usage.GetProperty("completion_tokens").GetInt32() >= 0);
    }

    [Fact]
    public async Task Completions_Returns_400_For_Missing_Messages()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["OPENROUTER_MOCK"] = "1",
        });

        using var client = factory.CreateClient();

        using var res = await client.PostAsJsonAsync("/api/v1/chat/completions", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";
        Assert.Contains("messages", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Completions_Returns_400_For_Empty_Messages_Array()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["OPENROUTER_MOCK"] = "1",
        });

        using var client = factory.CreateClient();

        using var res = await client.PostAsJsonAsync(
            "/api/v1/chat/completions",
            new
            {
                messages = Array.Empty<object>(),
            });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";
        Assert.Contains("empty", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_Returns_Healthy_With_Required_Fields()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var res = await client.GetAsync("/api/v1/chat/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("healthy", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("uptime_ms").GetInt64() >= 0);

        var timestamp = root.GetProperty("timestamp").GetString() ?? "";
        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}T", timestamp);
        var parsed = DateTimeOffset.Parse(timestamp);
        Assert.Equal(parsed.ToString("O"), timestamp);

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
    }

    [Fact]
    public async Task Health_Uptime_Increases_Over_Time()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var first = await client.GetFromJsonAsync<JsonElement>("/api/v1/chat/health");
        await Task.Delay(50);
        var second = await client.GetFromJsonAsync<JsonElement>("/api/v1/chat/health");

        Assert.True(
            second.GetProperty("uptime_ms").GetInt64() > first.GetProperty("uptime_ms").GetInt64());
    }

    private static WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?>? config = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                if (config is not null)
                {
                    builder.ConfigureAppConfiguration((_, cfg) =>
                        cfg.AddInMemoryCollection(config));
                }
            });
    }
}
