using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Ganimed.Overlays;

public abstract partial class SwitchableOverlayComponent : BaseOverlayComponent
{
    [DataField, AutoNetworkedField]
    public bool IsActive = true;

    [DataField]
    public virtual SoundSpecifier? ActivateSound { get; set; } =
        new SoundPathSpecifier("/Audio/_Ganimed/Items/Goggles/activate.ogg");

    [DataField]
    public virtual SoundSpecifier? DeactivateSound { get; set; } =
        new SoundPathSpecifier("/Audio/_Ganimed/Items/Goggles/deactivate.ogg");

    [DataField]
    public virtual string? ToggleAction { get; set; }

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}
