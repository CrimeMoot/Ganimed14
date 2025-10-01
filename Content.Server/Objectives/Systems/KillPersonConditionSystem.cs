using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireMaroon);
    }

    /// <summary>
    /// Новая логика:
    /// - смерть цели всегда даёт 100% и прогресс не спадает;
    /// - если цель жива, то проверяется условие шаттла.
    /// </summary>
    private float GetProgress(EntityUid target, bool requireMaroon)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        if (_mind.IsCharacterDeadIc(mind))
            return 1f;

        if (requireMaroon)
        {
            var targetMarooned = !_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) 
                                 || _mind.IsCharacterUnrevivableIc(mind);
            if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
                return 0f;

            // Always failed if the target needs to be marooned and the shuttle hasn't even arrived yet
            if (!_emergencyShuttle.EmergencyShuttleArrived)
                return 0f;

            // If the shuttle hasn't left, give 50% progress if the target isn't on the shuttle as a "almost there!"
            if (!_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 0.5f : 0f;

            // If the shuttle has already left, and the target isn't on it, 100%
            if (_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 1f : 0f;
        }

        // если maroon не требуется и цель не убивали → провал
        return 0f;
    }
}
