using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.VendingMachines
{
    /// <summary>
    /// Компонент для торговых автоматов департаментов.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DepartmentVendingComponent : Component
    {
        [DataField("department")]
        public string Department { get; set; } = string.Empty;

        [DataField("departmentAccount")]
        public string DepartmentAccount { get; set; } = string.Empty;

        [DataField("departmentDiscount")]
        public int DepartmentDiscount { get; set; } = 100; // 100% = бесплатно
    }
}