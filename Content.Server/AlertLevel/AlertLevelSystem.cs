using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access; // Ganimed edit
using Content.Shared.Access.Systems; // Ganimed edit
using Content.Shared.Access.Components; // Ganimed edit
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared._Ganimed.Access.AlertLevelAccess; // Ganimed edit
using Content.Shared.Popups; // Ganimed edit
using Robust.Shared.GameObjects; // Ganimed edit

namespace Content.Server.AlertLevel;

public sealed class AlertLevelSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!; // Ganimed edit
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Ganimed edit

    // Until stations are a prototype, this is how it's going to have to be.
    public const string DefaultAlertLevelSet = "stationAlerts";

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
    }

    public override void Update(float time)
    {
        var query = EntityQueryEnumerator<AlertLevelComponent>();

        while (query.MoveNext(out var station, out var alert))
        {
            if (alert.CurrentDelay <= 0)
            {
                if (alert.ActiveDelay)
                {
                    RaiseLocalEvent(new AlertLevelDelayFinishedEvent());
                    alert.ActiveDelay = false;
                }
                continue;
            }

            alert.CurrentDelay -= time;
        }
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        if (!TryComp<AlertLevelComponent>(args.Station, out var alertLevelComponent))
            return;

        if (!_prototypeManager.TryIndex(alertLevelComponent.AlertLevelPrototype, out AlertLevelPrototype? alerts))
        {
            return;
        }

        alertLevelComponent.AlertLevels = alerts;

        var defaultLevel = alertLevelComponent.AlertLevels.DefaultLevel;
        if (string.IsNullOrEmpty(defaultLevel))
        {
            defaultLevel = alertLevelComponent.AlertLevels.Levels.Keys.First();
        }

        SetLevel(args.Station, defaultLevel, false, false, true);
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.TryGetValue(typeof(AlertLevelPrototype), out var alertPrototypes)
            || !alertPrototypes.Modified.TryGetValue(DefaultAlertLevelSet, out var alertObject)
            || alertObject is not AlertLevelPrototype alerts)
        {
            return;
        }

        var query = EntityQueryEnumerator<AlertLevelComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.AlertLevels = alerts;

            if (!comp.AlertLevels.Levels.ContainsKey(comp.CurrentLevel))
            {
                var defaultLevel = comp.AlertLevels.DefaultLevel;
                if (string.IsNullOrEmpty(defaultLevel))
                {
                    defaultLevel = comp.AlertLevels.Levels.Keys.First();
                }

                SetLevel(uid, defaultLevel, true, true, true);
            }
        }

        RaiseLocalEvent(new AlertLevelPrototypeReloadedEvent());
    }

    public string GetLevel(EntityUid station, AlertLevelComponent? alert = null)
    {
        if (!Resolve(station, ref alert))
        {
            return string.Empty;
        }

        return alert.CurrentLevel;
    }

    public float GetAlertLevelDelay(EntityUid station, AlertLevelComponent? alert = null)
    {
        if (!Resolve(station, ref alert))
        {
            return float.NaN;
        }

        return alert.CurrentDelay;
    }

    /// <summary>
    /// Set the alert level based on the station's entity ID.
    /// </summary>
    /// <param name="station">Station entity UID.</param>
    /// <param name="level">Level to change the station's alert level to.</param>
    /// <param name="playSound">Play the alert level's sound.</param>
    /// <param name="announce">Say the alert level's announcement.</param>
    /// <param name="force">Force the alert change. This applies if the alert level is not selectable or not.</param>
    /// <param name="locked">Will it be possible to change level by crew.</param>
    public void SetLevel(EntityUid station, string level, bool playSound, bool announce, bool force = false,
        bool locked = false, MetaDataComponent? dataComponent = null, AlertLevelComponent? component = null)
    {
        if (!Resolve(station, ref component, ref dataComponent)
            || component.AlertLevels == null
            || !component.AlertLevels.Levels.TryGetValue(level, out var detail)
            || component.CurrentLevel == level)
        {
            return;
        }

        if (!force)
        {
            if (!detail.Selectable
                || component.CurrentDelay > 0
                || component.IsLevelLocked)
            {
                return;
            }

            component.CurrentDelay = _cfg.GetCVar(CCVars.GameAlertLevelChangeDelay);
            component.ActiveDelay = true;
        }

        var oldLevel = component.CurrentLevel; // Ganimed edit
        component.CurrentLevel = level;
        component.IsLevelLocked = locked;

        UpdateCardsAccessByAlertLevel(station, level); // Ganimed edit

        ShowAccessUpdatedPopup(station, oldLevel, level); // Ganimed edit

        var stationName = dataComponent.EntityName;

        var name = level; // ADT-Tweak

        if (Loc.TryGetString($"alert-level-{level}", out var locName))
        {
            name = locName; // ADT-Tweak
        }

        // Announcement text. Is passed into announcementFull.
        var announcement = detail.Announcement;

        if (Loc.TryGetString(detail.Announcement, out var locAnnouncement))
        {
            announcement = locAnnouncement;
        }

        // The full announcement to be spat out into chat.
        var announcementFull = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));

        var playDefault = false;
        if (playSound)
        {
            if (detail.Sound != null)
            {
                var filter = _stationSystem.GetInOwningStation(station);
                _audio.PlayGlobal(detail.Sound, filter, true, detail.Sound.Params);
            }
            else
            {
                playDefault = true;
            }
        }

        if (announce)
        {
            _chatSystem.DispatchStationAnnouncement(station, announcementFull, playDefaultSound: playDefault,
                colorOverride: detail.Color, sender: stationName);
        }

        RaiseLocalEvent(new AlertLevelChangedEvent(station, level));
    }

    // Ganimed edit start
    private void UpdateCardsAccessByAlertLevel(EntityUid station, string alertLevel)
    {
        var cards = EntityQueryEnumerator<AlertLevelAccessComponent>();

        while (cards.MoveNext(out var uid, out var alertAccessComp))
        {
            if (_stationSystem.GetOwningStation(uid) != station)
                continue;

            if (!TryComp<AccessComponent>(uid, out var accessComp))
                continue;

            // Сохраняем базовые теги при первом применении
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

    private void ShowAccessUpdatedPopup(EntityUid station, string oldLevel, string newLevel)
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
        // Ganimed edit stop
    }
}

public sealed class AlertLevelDelayFinishedEvent : EntityEventArgs
{}

public sealed class AlertLevelPrototypeReloadedEvent : EntityEventArgs
{}

public sealed class AlertLevelChangedEvent : EntityEventArgs
{
    public EntityUid Station { get; }
    public string AlertLevel { get; }

    public AlertLevelChangedEvent(EntityUid station, string alertLevel)
    {
        Station = station;
        AlertLevel = alertLevel;
    }
}
