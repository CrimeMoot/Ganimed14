using Content.Server._Ganimed.Objectives.Components;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Ganimed.Objectives.Systems;

/// <summary>
/// Система проверки количества сотрудников СБ для целей на убийство.
/// Можно отключать через CVar (objectives.sec_limit_enabled = false).
/// </summary>
public sealed class ObjectiveSecCountSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private DepartmentPrototype? _securityDepartment;

    public override void Initialize()
    {
        base.Initialize();

        if (_prototypeManager.TryIndex<DepartmentPrototype>("Security", out var secDept))
            _securityDepartment = secDept;
        else
            Log.Error("Security department prototype not found!");

        // Перехватываем убийство ДО выбора цели
        SubscribeLocalEvent<ObjectiveSecCountComponent, ObjectiveAssignedEvent>(
            OnObjectiveAssignedPre,
            before: new[] { typeof(Content.Server.Objectives.Systems.PickObjectiveTargetSystem) });

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (_prototypeManager.TryIndex<DepartmentPrototype>("Security", out var secDept))
            _securityDepartment = secDept;
    }

    // Выполняется до подбора цели. Отменяет выдачу цели на убийство при нехватке СБ.
    private void OnObjectiveAssignedPre(Entity<ObjectiveSecCountComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!_cfg.GetCVar(CCVars.SecObjectivesLimitEnabled))
            return;

        if (!HasComp<KillPersonConditionComponent>(ent.Owner) || !HasComp<TargetObjectiveComponent>(ent.Owner))
            return;

        // Определяем станцию по текущему владельцу
        if (args.Mind.OwnedEntity is not { } owned)
            return;

        var station = _stationSystem.GetOwningStation(owned);
        if (station == null)
            return;

        var secCount = CountSecurityOfficers(station.Value);

        if (secCount < ent.Comp.MinSec)
        {
            // Блокируем выдачу ДО назначения цели
            args.Cancelled = true;
            Log.Info($"Kill objective blocked before assignment: Security={secCount}/{ent.Comp.MinSec} on {ToPrettyString(station.Value)}");
        }
    }

    // Подсчет живых сотрудников СБ на станции через департамент Security
    private int CountSecurityOfficers(EntityUid station)
    {
        if (_securityDepartment == null)
        {
            Log.Warning("Security department not loaded, cannot count officers");
            return 0;
        }

        var secCount = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } entity)
                continue;

            // На той же станции?
            var entityStation = _stationSystem.GetOwningStation(entity);
            if (entityStation != station)
                continue;

            // Жив?
            if (TryComp<MobStateComponent>(entity, out var mobState) &&
                mobState.CurrentState != MobState.Alive)
                continue;

            // Получаем mind и job корректно (MindTryGetJob принимает mind-uid)
            if (!_mindSystem.TryGetMind(entity, out var mindId, out _))
                continue;

            if (!_jobSystem.MindTryGetJob(mindId, out var job) || job == null)
                continue;

            // Принадлежит департаменту Security?
            if (_securityDepartment.Roles.Contains(job.ID))
                secCount++;
        }

        return secCount;
    }
}
