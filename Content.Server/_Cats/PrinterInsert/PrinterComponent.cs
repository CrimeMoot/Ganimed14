namespace Content.Server._Cats.PrinterInsert
{
    [RegisterComponent]
    public sealed partial class PrinterComponent : Component
    {
        [DataField]
        public string UserName = "";

        [DataField]
        public string UserJob = "";
    }
}