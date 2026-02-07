using Microsoft.Extensions.AI;
using Fhas.Agent.Skills;

namespace Fhas.Agent.Runtime;

internal static class SkillToolAdapter
{
    public static AITool ToTool(ISkill skill)
    {
        // Each skill is exposed as a function tool with a single string input.
        // The Agent Framework will handle tool selection and invocation automatically.
        return AIFunctionFactory.Create(
            (string input, CancellationToken ct) => skill.InvokeAsync(input, ct),
            name: skill.Info.Id,
            description: skill.Info.Description,
            serializerOptions: null);
    }
}

