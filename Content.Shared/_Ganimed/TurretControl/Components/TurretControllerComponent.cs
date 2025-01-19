using Robust.Shared.Prototypes;

namespace Content.Shared._Ganimed.TurretControl.Components;

[RegisterComponent]
public sealed partial class TurretControllerComponent : Component
{
    [DataField]
    public ComponentRegistry RequiredComponents = [];
}