using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Shared.Lathe;
using Robust.Shared.Timing;
using Content.Shared.GameTicking;
using Content.Shared.UserInterface;
using Content.Server.Roles.Jobs;
using Content.Shared.Mind.Components;
using Content.Server.Mind;

namespace Content.Server._Cats.PrinterInsert;

public sealed class PrinterInsertSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _minds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, LatheResultSpawnEvent>(PaperSpawn);
        SubscribeLocalEvent<PrinterComponent, BeforeActivatableUIOpenEvent>(UpdateUserInterfaceState);
    }

    private void UpdateUserInterfaceState(Entity<PrinterComponent> uid, ref BeforeActivatableUIOpenEvent args)
    {
        if (!_entityManager.TryGetComponent<MindContainerComponent>(args.User, out var _))
        {
            uid.Comp.UserName = "";
            uid.Comp.UserJob = "";

            return;
        }

        uid.Comp.UserName = Name(args.User);

        if (_minds.TryGetMind(args.User, out var mindId, out var _))
        {

            if (_jobs.MindTryGetJobName(mindId, out var jobName))
            {
                uid.Comp.UserJob = jobName;
                return;
            }
        }

        uid.Comp.UserJob = "";

    }

    private void PaperSpawn(Entity<PaperComponent> entity, ref LatheResultSpawnEvent args)
    {
        var station = _stationSystem.GetOwningStation(entity);
        if (station != null)
        {
            var data = DateTime.Today.ToShortDateString().Replace("202", "302");
            var time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan).ToString("hh\\:mm\\:ss");
            var datatime = "Время от начала смены и дата: " + time + " " + data;
            _paperSystem.SetContent(entity, entity.Comp.Content.Replace("Station XX-000", Name(station.Value)));
            _paperSystem.SetContent(entity, entity.Comp.Content.Replace("Время от начала смены и дата:", datatime));

            if (_entityManager.TryGetComponent<PrinterComponent>(args.Lathe, out var Comp))
            {
                _paperSystem.SetContent(entity, entity.Comp.Content.Replace("Составитель документа:", "Составитель документа: " + Comp.UserName));
                _paperSystem.SetContent(entity, entity.Comp.Content.Replace("Должность составителя:", "Должность составителя: " + Comp.UserJob));
            }



        }

    }
}