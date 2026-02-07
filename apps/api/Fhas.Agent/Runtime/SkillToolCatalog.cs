using Fhas.Agent.Skills;
using Microsoft.Extensions.AI;

namespace Fhas.Agent.Runtime;

public sealed class SkillToolCatalog
{
    public SkillToolCatalog(IEnumerable<ISkill> skills)
    {
        Tools = skills
            .Select(SkillToolAdapter.ToTool)
            .ToList();
    }

    public IList<AITool> Tools { get; }
}
