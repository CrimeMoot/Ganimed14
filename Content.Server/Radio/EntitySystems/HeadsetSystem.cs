using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Radio.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Verbs; // Ganimed edit
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility; // Ganimed edit
using Content.Server.ADT.Language;  // ADT Languages
using Content.Shared.Popups; // Ganimed edit
using Robust.Shared.GameObjects; // Ganimed edit

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly LanguageSystem _language = default!;  // ADT Languages
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Ganimed edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);

        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);

        SubscribeLocalEvent<HeadsetComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs); // Ganimed edit 
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        component.IsEquipped = false;
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp(Transform(uid).ParentUid, out ActorComponent? actor))
        {
            // ADT Languages start
            if (_language.CanUnderstand(Transform(uid).ParentUid, args.Language))
                _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
            else
                _netMan.ServerSendMessage(args.UnknownLanguageChatMsg, actor.PlayerSession.Channel);
            // ADT Languages end
        }
    }

    private void OnEmpPulse(EntityUid uid, HeadsetComponent component, ref EmpPulseEvent args)
    {
        if (component.Enabled)
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }

    // Ganimed edit start

    private void OnGetAltVerbs(EntityUid uid, HeadsetComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (comp.RadioTextIncrease <= 0)
            return;

        var verb = new AlternativeVerb
        {
            Act = () =>
            {
                comp.RadioBoostEnabled = !comp.RadioBoostEnabled;

                var msg = comp.RadioBoostEnabled
                    ? Loc.GetString("headset-radio-fontboost-on")
                    : Loc.GetString("headset-radio-fontboost-off");

                _popup.PopupEntity(msg, args.User, PopupType.Small);
            },
            Text = comp.RadioBoostEnabled
                ? Loc.GetString("headset-radio-fontboost-disable")
                : Loc.GetString("headset-radio-fontboost-enable"),
        };

        args.Verbs.Add(verb);
    }
    // Ganimed edit end
}