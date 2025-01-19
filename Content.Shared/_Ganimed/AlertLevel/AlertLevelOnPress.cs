using Robust.Shared.Prototypes;

namespace Content.Shared._Ganimed.AlertLevelOnPress;

public abstract partial class SharedAlertLevelOnPressSystem : EntitySystem
{
}

[RegisterComponent]
[Access(typeof(SharedAlertLevelOnPressSystem))]
public sealed partial class AlertLevelOnPressComponent : Component
{
    [DataField(required: true)]
    public string AlertLevelOnActivate = string.Empty;
}