using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._Ganimed.Access.AlertLevelAccess;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Station.Systems;

namespace Content.Server._Ganimed.AlertLevel
{
    public sealed class AlertLevelAccessSystem : EntitySystem
    {
        [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
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

                HashSet<ProtoId<AccessLevelPrototype>>? levelTags = alertLevel.ToLowerInvariant() switch
                {
                    "blue" => alertAccessComp.Blue,
                    "violet" => alertAccessComp.Violet,
                    "red" => alertAccessComp.Red,
                    "gamma" => alertAccessComp.Gamma,
                    "delta" => alertAccessComp.Delta,
                    "green" => alertAccessComp.BaseTags.Count > 0 ? alertAccessComp.BaseTags : new HashSet<ProtoId<AccessLevelPrototype>>(),
                    _ => null
                };

                if (levelTags == null)
                    continue;

                HashSet<ProtoId<AccessLevelPrototype>> combinedTags;

                if (alertLevel.ToLowerInvariant() == "green")
                {
                    combinedTags = new HashSet<ProtoId<AccessLevelPrototype>>(alertAccessComp.BaseTags);
                }
                else
                {
                    combinedTags = new HashSet<ProtoId<AccessLevelPrototype>>(alertAccessComp.BaseTags);
                    combinedTags.UnionWith(levelTags);
                }

                _accessSystem.TrySetTags(uid, combinedTags, accessComp);
            }
        }
    }
}