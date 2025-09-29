using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Robust.Shared.Configuration;
using Content.Shared.Roles;
using Content.Shared.Mind;
using Content.Shared._Ganimed.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared._Ganimed.StationEvents.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Utility;

/// <summary>
/// Глобальный гейт для игровых правил: если не хватает людей в департаментах — событие не запускается.
/// Работает для любых игровых событий с DepartmentPlayerLimitComponent.
/// По умолчанию игнорирует правила, добавленные пресетом до старта раунда (см. IgnoreOnRoundStart).
/// </summary>
public sealed class DepartmentPlayerLimitSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Вешаемся на добавление любого game rule. Это происходит до его Started().
        SubscribeLocalEvent<DepartmentPlayerLimitComponent, GameRuleAddedEvent>(OnRuleAdded);
    }

    private void OnRuleAdded(EntityUid uid, DepartmentPlayerLimitComponent comp, ref GameRuleAddedEvent args)
    {
        if (!_cfg.GetCVar(CCVars.DepartmentLimitEnabled))
            return;

        // Если надо игнорить пресетные (ранне-добавленные) правила - выходим до InRound.
        if (comp.IgnoreOnRoundStart && _ticker.RunLevel != GameRunLevel.InRound)
            return;

        // Меньше лимита — ничего не делаем.
        if (comp.MinLimit <= 0 || comp.Departments.Count == 0)
            return;

        // Считаем людей по департаментам
        var onlineByDept = CountOnlineByDepartments(comp.Departments);

        if (onlineByDept >= comp.MinLimit)
            return;

        var deptNames = string.Join(", ", comp.Departments);

        _chatManager.SendAdminAnnouncementColor(
                            $"[System] Отмена события {ToPrettyString(uid)}: участников {deptNames} меньше {onlineByDept} из {comp.MinLimit} желаемых",
                            colorOverrid: Color.White
                        );

        // Удаляем правило, чтобы его Started() не отработал вовсе.
        QueueDel(uid);
    }

    /// <summary>
    /// Считает суммарное число игроков онлайн, чьи должности входят в указанные департаменты.
    /// </summary>
    private int CountOnlineByDepartments(IReadOnlyList<ProtoId<DepartmentPrototype>> departments)
    {
        // Собираем допустимые JobIds из департаментов
        var validJobs = new HashSet<string>();
        foreach (var deptId in departments)
        {
            if (!_protos.TryIndex(deptId, out DepartmentPrototype? dept))
                continue;

            foreach (var jobId in dept.Roles)
                validJobs.Add(jobId);
        }

        var count = 0;

        foreach (var sess in _players.Sessions)
        {
            // Должна быть сущность сессии
            if (sess.AttachedEntity is not { } ent)
                continue;

            // Получаем mind и сам mind-компонент
            if (!_minds.TryGetMind(ent, out var mindId, out var mind))
                continue;

            // Mind должен быть в теле
            if (mind.OwnedEntity is not { } body)
                continue;

            // Должен быть живой моб
            if (!TryComp<MobStateComponent>(body, out var mobState) ||
                mobState.CurrentState != MobState.Alive)
                continue;

            // Должность берём по mind
            if (!_jobs.MindTryGetJob(mindId, out var job) || job == null)
                continue;

            if (validJobs.Contains(job.ID))
                count++;
        }

        return count;
    }
}