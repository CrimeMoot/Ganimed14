using Content.Server.Objectives.Systems;

namespace Content.Server._Ganimed.Objectives.Components;

[RegisterComponent]
public sealed partial class ObjectiveSecCountComponent : Component
{
    [DataField]
    public int MinSec = 0;
}