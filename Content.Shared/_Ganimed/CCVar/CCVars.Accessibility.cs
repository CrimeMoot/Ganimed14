using Robust.Shared.Configuration;

namespace Content.Shared._Ganimed.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Переключение ограничений депортамента
    /// </summary>
    public static readonly CVarDef<bool> DepartmentLimitEnabled =
        CVarDef.Create("game.departmentlimit_event", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}