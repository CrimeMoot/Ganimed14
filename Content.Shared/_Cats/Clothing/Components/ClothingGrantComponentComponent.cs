using Robust.Shared.Prototypes;

namespace Content.Shared._Cats.Clothing
{
    [RegisterComponent]
    public sealed partial class ClothingGrantComponentComponent : Component
    {
        [DataField("component")]
        [AlwaysPushInheritance]
        public ComponentRegistry Components { get; private set; } = new();

        public bool IsActive = false;
    }
}