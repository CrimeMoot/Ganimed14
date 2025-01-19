namespace Content.Shared._Ganimed.Clothing;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    [DataField("tag")]
    public string Tag = "";

    public bool IsActive = false;
}