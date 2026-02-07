namespace Fhas.Agent.Policies;

// Minimal "Rule-of-Constraints" (FPF-inspired) envelope for future agent execution.
// Keep this small and evolvable; wire into runtime enforcement later.
public sealed record RuleOfConstraints(
    IReadOnlyList<string> AllowedTools,
    int? MaxToolCalls,
    TimeSpan? MaxWallTime
);

