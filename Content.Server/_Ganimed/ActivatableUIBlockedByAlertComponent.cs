using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server._Ganimed
{
    [RegisterComponent]
    public sealed partial class ActivatableUIBlockedByAlertComponent : Component
    {
        /// <summary>
        /// Ganimed
        /// Блокирует доступ к UI. Если не указана, то только при Red тревоге. Но можно указать любой имеющийся.
        /// </summary>
        [DataField("BlockedAlertLevelCode")]
        public string BlockedAlertLevelCode = "red";
    }
}