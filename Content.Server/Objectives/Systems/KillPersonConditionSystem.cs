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
    /// - если цель жива, то проверяется условие шаттла (maroon).
    /// </summary>
    private float GetProgress(EntityUid target, bool requireMaroon)
    {
        // удалена/распилена/иначе потеряна сущность — считается как убитая
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        // один раз умер — цель выполнена
        if (_mind.IsCharacterDeadIc(mind))
            return 1f;

        // проверяем maroon
        if (requireMaroon)
        {
            var targetMarooned = !_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) 
                                 || _mind.IsCharacterUnrevivableIc(mind);

            // если шаттлы отключены, maroon недостижим
            if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
                return 0f;

            // шаттл ещё не прибыл → прогресс 0
            if (!_emergencyShuttle.EmergencyShuttleArrived)
                return 0f;

            // шаттл ещё не улетел → можно дать 50%, если цель уже не на нём
            if (!_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 0.5f : 0f;

            // шаттл улетел → если цели на нём нет, то 100%
            if (_emergencyShuttle.ShuttlesLeft)
                return targetMarooned ? 1f : 0f;
        }

        // если maroon не требуется и цель жива → провал
        return 0f;
    }
}