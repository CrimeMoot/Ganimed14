using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Shared.StationEvents.Components;

/// <summary>
/// Ограничитель события по минимальному числу игроков в указанных департаментах.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DepartmentPlayerLimitComponent : Component
{
    /// <summary>
    /// Список департаментов, по которым считаем игроков.
    /// Значения — ProtoId<DepartmentPrototype> (например: "Science", "Security").
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DepartmentPrototype>> Departments = new();

    /// <summary>
    /// Минимальное суммарное количество игроков по указанным департаментам.
    /// </summary>
    [DataField(required: true)]
    public int MinLimit = 0;

    /// <summary>
    /// Если true (по умолчанию) — НЕ проверять правила, добавленные пресетом ДО старта раунда.
    /// Это оставляет в покое game_preset со множеством преднастроенных правил, потому что я не знаю — почему оно ломает сервер.
    /// </summary>
    [DataField]
    public bool IgnoreOnRoundStart = true;
}