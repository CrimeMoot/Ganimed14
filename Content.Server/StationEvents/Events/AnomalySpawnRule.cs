using Content.Server.Anomaly;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
// Ganimed edit start
using Content.Shared.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
// Ganimed edit end

namespace Content.Server.StationEvents.Events;

public sealed class AnomalySpawnRule : StationEventSystem<AnomalySpawnRuleComponent>
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!; // Ganimed edit
    [Dependency] private readonly SharedMindSystem _mindSystem = default!; // Ganimed edit
    [Dependency] private readonly SharedJobSystem _jobSystem = default!; // Ganimed edit
    [Dependency] private readonly IPrototypeManager _prototypes = default!; // Ganimed edit
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Ganimed edit

    private DepartmentPrototype? _scienceDepartment; // Ganimed edit

    // Ganimed edit start
    public override void Initialize()
    {
        base.Initialize();

        if (_prototypes.TryIndex<DepartmentPrototype>("Science", out var sciDept))
            _scienceDepartment = sciDept;
        else
            Log.Error("Science department prototype not found!");
    }
    // Ganimed edit end

    protected override void Added(EntityUid uid, AnomalySpawnRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var str = Loc.GetString("anomaly-spawn-event-announcement",
            ("sighting", Loc.GetString($"anomaly-spawn-sighting-{RobustRandom.Next(1, 6)}")));
        stationEvent.StartAnnouncement = str;

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, AnomalySpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;
        // Ganimed edit start
        EntityUid stationUid = chosenStation.Value;

        // Проверка через CVar — если включена, требуем минимум указанное количество учёных
        if (_cfg.GetCVar(CCVars.AutoEventsAnomalySpawnNeedRnd))
        {
            var scientistCount = CountScientists(stationUid);
            if (scientistCount < component.MinScientists)
            {
                Log.Info($"Anomaly spawn blocked: Scientists={scientistCount}/{component.MinScientists} on {ToPrettyString(stationUid)}");
                return;
            }
        }
        // Ganimed edit end

        if (!TryComp<StationDataComponent>(stationUid, out var stationData)) // Ganimed edit
            return;

        var gridNullable = StationSystem.GetLargestGrid(stationData); // Ganimed edit
        if (gridNullable == null) // Ganimed edit
            return;

        EntityUid gridUid = gridNullable.Value; // Ganimed edit

        var amountToSpawn = 1;
        for (var i = 0; i < amountToSpawn; i++)
        {
            _anomaly.SpawnOnRandomGridLocation(gridUid, component.AnomalySpawnerPrototype);
        }
    }

    // Ganimed edit start
    private int CountScientists(EntityUid station)
    {
        if (_scienceDepartment == null)
            return 0;

        var count = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } entity)
                continue;

            var entityStation = StationSystem.GetOwningStation(entity);
            if (entityStation != station)
                continue;

            if (!_mindSystem.TryGetMind(entity, out var mindId, out _))
                continue;

            if (!_jobSystem.MindTryGetJob(mindId, out var job) || job == null)
                continue;

            if (_scienceDepartment.Roles.Contains(job.ID))
                count++;
        }

        return count;
    }
    // Ganimed edit end
}