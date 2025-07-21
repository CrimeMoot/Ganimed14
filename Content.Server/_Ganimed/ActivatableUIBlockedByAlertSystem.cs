using System;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Content.Server.Popups;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;

namespace Content.Server._Ganimed
{
    public sealed partial class ActivatableUIBlockedByAlertSystem : EntitySystem
    {
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
        }

        private void OnBoundUiMessageAttempt(BoundUserInterfaceMessageAttempt ev)
        {
            if (ev.Message is not OpenBoundInterfaceMessage)
                return;

            if (!EntityManager.TryGetComponent<ActivatableUIBlockedByAlertComponent>(ev.Target, out var blockComp))
                return;

            var station = _stationSystem.GetOwningStation(ev.Target);
            if (station == null)
                return;

            var currentAlert = _alertLevelSystem.GetLevel(station.Value);

            if (string.Equals(currentAlert, blockComp.BlockedAlertLevelCode, StringComparison.OrdinalIgnoreCase))
            {
                ev.Cancel();

                _popupSystem.PopupEntity(
                    Loc.GetString("ui-blocked-by-alert-popup-message"),
                    ev.Target,
                    ev.Actor,
                    PopupType.LargeCaution);

                return;
            }
        }
    }
}