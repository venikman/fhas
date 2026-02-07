namespace Fhas.Agent.Skills;

public sealed class SkillRegistry(IEnumerable<ISkill> skills)
{
    private readonly IReadOnlyDictionary<string, ISkill> _byId =
        skills.ToDictionary(s => s.Info.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SkillInfo> List() =>
        _byId.Values
            .Select(s => s.Info)
            .OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool TryGet(string id, out ISkill? skill) =>
        _byId.TryGetValue(id, out skill);
}

