namespace Content.Shared._Ganimed.BackStab;

[RegisterComponent]
public sealed partial class BackStabComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageMultiplier = 1.4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Angle Tolerance = Angle.FromDegrees(45d);
}