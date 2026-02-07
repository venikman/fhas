namespace Fhas.Agent.Skills;

public sealed class EchoSkill : ISkill
{
    public SkillInfo Info => new("echo", "Echoes input (starter skill scaffold).");

    public Task<string> InvokeAsync(string input, CancellationToken cancellationToken)
        => Task.FromResult(input);
}

