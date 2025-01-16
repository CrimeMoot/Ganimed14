namespace Content.Shared._Ganimed.Overlays;

public abstract partial class BaseOverlayComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual float Strength { get; set; } = 2f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual float Noise { get; set; } = 0.5f;

}
