using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._Ganimed.Access.AlertLevelAccess;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Station.Systems;

namespace Content.Server._Ganimed.AlertLevel
{
    public sealed class AlertLevelAccessSystem : EntitySystem
    {
        [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        public void UpdateCardsAccessByAlertLevel(EntityUid station, string alertLevel)
        {
            var cards = EntityQueryEnumerator<AlertLevelAccessComponent>();

            while (cards.MoveNext(out var uid, out var alertAccessComp))
            {
                if (_stationSystem.GetOwningStation(uid) != station)
                    continue;

                if (!TryComp<AccessComponent>(uid, out var accessComp))
                    continue;

                if (alertAccessComp.BaseTags.Count == 0)
                {
                    alertAccessComp.BaseTags = new HashSet<ProtoId<AccessLevelPrototype>>(accessComp.Tags);
                }

                HashSet<ProtoId<AccessLevelPrototype>>? tagsToSet = alertLevel.ToLowerInvariant() switch
                {
                    "blue" => alertAccessComp.Blue,
                    "violet" => alertAccessComp.Violet,
                    "red" => alertAccessComp.Red,
                    "gamma" => alertAccessComp.Gamma,
                    "delta" => alertAccessComp.Delta,
                    "green" => alertAccessComp.BaseTags.Count > 0 ? alertAccessComp.BaseTags : new HashSet<ProtoId<AccessLevelPrototype>>(),
                    _ => null
                };

                if (tagsToSet == null)
                    continue;

                _accessSystem.TrySetTags(uid, tagsToSet, accessComp);
            }
        }

        public void ShowAccessUpdatedPopup(EntityUid station, string oldLevel, string newLevel)
        {
            var cards = EntityQueryEnumerator<AlertLevelAccessComponent>();

            var newLevelName = newLevel;
            if (Loc.TryGetString($"alert-level-{newLevel}", out var locName))
                newLevelName = locName;

            string message;

            if (newLevel.ToLowerInvariant() == "green")
            {
                message = Loc.GetString("alert-level-access-lowered-popup");
            }
            else if (oldLevel.ToLowerInvariant() == "green")
            {
                message = Loc.GetString("alert-level-access-raised-popup");
            }
            else
            {
                message = Loc.GetString("alert-level-access-changed-popup");
            }

            while (cards.MoveNext(out var uid, out var alertAccessComp))
            {
                if (_stationSystem.GetOwningStation(uid) != station)
                    continue;

                _popup.PopupEntity(message, uid, PopupType.Large);
            }
        }
    }
}