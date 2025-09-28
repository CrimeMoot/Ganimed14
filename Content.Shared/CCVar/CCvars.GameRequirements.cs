using Content.Shared.Roles;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Ganimed edit
    ///     Включает или выключает ограничение по количеству СБ для целей на убийств
    /// </summary>
    public static readonly CVarDef<bool> SecObjectivesLimitEnabled =
        CVarDef.Create("game_requirements.antag_objectives_killobjective_need_sec", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    ///     Ganimed edit
    ///     Включает/выключает требование минимального количества учёных для спавна аномалий
    /// </summary>
    public static readonly CVarDef<bool> AutoEventsAnomalySpawnNeedRnd =
        CVarDef.Create("game_requirements.autoevents_anomaly_spawn_need_rnd", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

}