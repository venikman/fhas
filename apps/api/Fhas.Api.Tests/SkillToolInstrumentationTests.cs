using System.Diagnostics;
using System.Text.Json;
using Fhas.Agent.Runtime;
using Fhas.Agent.Skills;
using Microsoft.Extensions.AI;

namespace Fhas.Api.Tests;

public sealed class SkillToolInstrumentationTests
{
    [Fact]
    public async Task Skill_Tool_Emits_Activity_On_Invoke()
    {
        var activities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OpenRouterAgentRunner.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => activities.Add(a),
        };
        ActivitySource.AddActivityListener(listener);

        var catalog = new SkillToolCatalog(new ISkill[] { new EchoSkill() });
        var tool = Assert.Single(catalog.Tools);

        var fn = Assert.IsAssignableFrom<AIFunction>(tool);
        var result = await fn.InvokeAsync(
            new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "hello" }),
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("hello", json.GetString());

        Assert.Contains(
            activities,
            a => a.DisplayName == "skill.invoke" &&
                 a.Tags.Any(t => t.Key == "fhas.skill.id" && (string?)t.Value == "echo"));
    }
}
