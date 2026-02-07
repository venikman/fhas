namespace Fhas.Agent.Skills;

public sealed record SkillInfo(string Id, string Description);

public interface ISkill
{
    SkillInfo Info { get; }

    Task<string> InvokeAsync(string input, CancellationToken cancellationToken);
}

