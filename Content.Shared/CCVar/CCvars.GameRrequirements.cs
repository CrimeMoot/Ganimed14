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
        CVarDef.Create("game.objectives_sec_limit_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

}