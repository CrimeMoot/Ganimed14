using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.ADT.SizeAttribute
{
    [RegisterComponent]
    public sealed partial class SizeAttributeWhitelistComponent : Component
    {
        // Short
        [DataField("short")]
        public bool Short = false;

        [DataField("shortscale")]
        public float ShortScale = 1f;

        [DataField("shortDensity")]
        public float ShortDensity = 185f;

        [DataField("shortPseudoItem")]
        public bool ShortPseudoItem = false;

        [DataField("shortCosmeticOnly")]
        public bool ShortCosmeticOnly = true;

        // Tall
        [DataField("tall")]
        public bool Tall = false;

        [DataField("tallscale")]
        public float TallScale = 1f;

        [DataField("tallDensity")]
        public float TallDensity = 185f;

        [DataField("tallPseudoItem")]
        public bool TallPseudoItem = false;

        [DataField("tallCosmeticOnly")]
        public bool TallCosmeticOnly = true;
    }
}
