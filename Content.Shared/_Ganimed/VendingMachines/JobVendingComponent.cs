using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.VendingMachines;

/// <summary>
/// Компонент для торговых автоматов, которые дают скидку конкретным профессиям (JobPrototype).
/// </summary>
[RegisterComponent]
public sealed partial class JobVendingComponent : Component
{
    /// <summary>
    /// Айди профессии (JobPrototype.id), для которой действует скидка.
    /// Например: "Chef", "Bartender".
    /// </summary>
    [DataField("job")]
    public string Job { get; set; } = string.Empty;

    /// <summary>
    /// Скидка в процентах (100 = бесплатно).
    /// </summary>
    [DataField("jobDiscount")]
    public int JobDiscount { get; set; } = 100;
}
