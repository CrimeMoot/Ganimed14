using Robust.Shared.GameStates;

namespace Content.Shared._Ganimed.Jammer;

[RegisterComponent]
public sealed partial class SetJammerOnSpawnComponent : Component
{
    [DataField("duration")]
    public TimeSpan Duration = TimeSpan.FromMinutes(10);
}