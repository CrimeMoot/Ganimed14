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

        args.Progress = GetProgress(uid, target.Value, comp);
    }

    /// <summary>
    /// Логика цели "убить персонажа":
    /// - если цель когда-либо была убита, прогресс навсегда = 100%;
    /// - если цель жива, проверяется условие maroon (шаттл);
    /// - если цель удалена или gibbed, засчитывается как убитая.
    /// </summary>
    private float GetProgress(EntityUid uid, EntityUid target, KillPersonConditionComponent comp)
    {
        if (comp.KilledOnce)
            return 1f;

        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
        {
            comp.KilledOnce = true;
            return 1f;
        }

        if (_mind.IsCharacterDeadIc(mind))
        {
            comp.KilledOnce = true;
            return 1f;
        }

        if (comp.RequireMaroon)
        {
            var targetMarooned = !_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value)
                                 || _mind.IsCharacterUnrevivableIc(mind);

            if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
                return 0f;

            if (!_emergencyShuttle.EmergencyShuttleArrived)
                return 0f;

            if (!_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 0.5f : 0f;

            if (_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 1f : 0f;
        }

        return 0f;
    }
}