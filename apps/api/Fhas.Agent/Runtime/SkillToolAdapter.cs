using System.Diagnostics;
using Microsoft.Extensions.AI;
using Fhas.Agent.Skills;

namespace Fhas.Agent.Runtime;

internal static class SkillToolAdapter
{
    private static readonly ActivitySource ActivitySource = new(OpenRouterAgentRunner.ActivitySourceName);

    public static AITool ToTool(ISkill skill)
    {
        // Each skill is exposed as a function tool with a single string input.
        // The Agent Framework will handle tool selection and invocation automatically.
        return AIFunctionFactory.Create(
            async (string input, CancellationToken ct) =>
            {
                using var activity = ActivitySource.StartActivity("skill.invoke", ActivityKind.Internal);
                activity?.SetTag("gen_ai.tool.name", skill.Info.Id);
                activity?.SetTag("fhas.skill.id", skill.Info.Id);

                try
                {
                    return await skill.InvokeAsync(input, ct);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity?.SetTag("exception.type", ex.GetType().FullName);
                    activity?.SetTag("exception.message", ex.Message);
                    throw;
                }
            },
            name: skill.Info.Id,
            description: skill.Info.Description,
            serializerOptions: null);
    }
}
