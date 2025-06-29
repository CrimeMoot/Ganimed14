using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._Ganimed.Jammer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationJammerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? JammerEndTime;
}