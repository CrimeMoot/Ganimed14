using Content.Server._Ganimed.Objectives.Components;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Ganimed.Objectives.Systems;

/// <summary>
/// Система проверки количества сотрудников СБ для kill-объективов.
/// Блокирует выдачу целей убийства при недостаточном количестве офицеров СБ.
/// </summary>
public sealed class ObjectiveSecCountSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    
    private DepartmentPrototype? _securityDepartment;
    
    public override void Initialize()
    {
        base.Initialize();
        
        if (_prototypeManager.TryIndex<DepartmentPrototype>("Security", out var secDept))
        {
            _securityDepartment = secDept;
        }
        else
        {
            Log.Error("Security department prototype not found!");
        }
        
        SubscribeLocalEvent<ObjectiveSecCountComponent, ObjectiveAfterAssignEvent>(OnObjectiveAfterAssign);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }
    
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (_prototypeManager.TryIndex<DepartmentPrototype>("Security", out var secDept))
        {
            _securityDepartment = secDept;
        }
    }

    private void OnObjectiveAfterAssign(EntityUid uid, ObjectiveSecCountComponent component, ref ObjectiveAfterAssignEvent args)
    {
        // Проверяем только kill-объективы
        if (!HasComp<KillPersonConditionComponent>(uid))
            return;

        // Получаем цель объектива
        if (!TryComp<TargetObjectiveComponent>(uid, out var targetComp) || targetComp.Target == null)
            return;

        // Определяем станцию
        EntityUid? station = null;
        
        // Пытаемся получить станцию от цели
        if (TryComp<MindComponent>(targetComp.Target.Value, out var targetMind) && targetMind.OwnedEntity != null)
        {
            station = _stationSystem.GetOwningStation(targetMind.OwnedEntity.Value);
        }
        
        // Если не удалось - пытаемся получить станцию от владельца объектива
        if (station == null && args.Mind != EntityUid.Invalid)
        {
            if (TryComp<MindComponent>(args.Mind, out var ownerMind) && ownerMind.OwnedEntity != null)
            {
                station = _stationSystem.GetOwningStation(ownerMind.OwnedEntity.Value);
            }
        }

        if (station == null)
        {
            Log.Warning($"Cannot determine station for objective {ToPrettyString(uid)}");
            return;
        }

        // Подсчитываем сотрудников СБ
        var secCount = CountSecurityOfficers(station.Value);
        
        if (secCount < component.MinSec)
        {
            Log.Info($"Kill objective {ToPrettyString(uid)} BLOCKED and REMOVED: only {secCount} security officers (minimum: {component.MinSec})");

            if (TryComp<MindComponent>(args.Mind, out var mind))
            {
                if (mind.Objectives.Contains(uid))
                {
                    mind.Objectives.Remove(uid);
                    Log.Info($"Removed objective {ToPrettyString(uid)} from mind {ToPrettyString(args.Mind)}");
                }

                EntityManager.QueueDeleteEntity(uid);
                Log.Info($"Deleted objective entity {ToPrettyString(uid)}");

            }
        }
    }

    /// <summary>
    /// Подсчитывает количество живых сотрудников СБ на станции
    /// </summary>
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
            if (session.AttachedEntity == null)
                continue;
                
            var entity = session.AttachedEntity.Value;
            
            // Проверяем что игрок на нужной станции
            var entityStation = _stationSystem.GetOwningStation(entity);
            if (entityStation != station)
                continue;
                
            // Проверяем что игрок жив
            if (TryComp<MobStateComponent>(entity, out var mobState))
            {
                if (mobState.CurrentState != MobState.Alive)
                    continue;
            }
            
            // Получаем должность
            if (!_mindSystem.TryGetMind(entity, out var mindId, out _))
                continue;

            if (!_jobSystem.MindTryGetJob(mindId, out var job) || job == null)
                continue;

            if (IsJobInSecurityDepartment(job.ID))
            {
                secCount++;
                Log.Debug($"Counted security officer: {session.Name} ({job.ID})");
            }
        }

        return secCount;
    }

    private bool IsJobInSecurityDepartment(string jobId)
    {
        if (_securityDepartment == null)
            return false;

        return _securityDepartment.Roles.Contains(jobId);
    }

    public int GetSecurityCount(EntityUid station)
    {
        return CountSecurityOfficers(station);
    }
}

[ByRefEvent]
public record struct ObjectiveAfterAssignEvent(EntityUid Mind, EntityUid Objective, EntityUid Agent);