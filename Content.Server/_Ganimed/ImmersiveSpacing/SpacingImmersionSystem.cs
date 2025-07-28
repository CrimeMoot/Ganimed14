using Content.Shared._Ganimed.Components;
using Content.Server.Popups;

namespace Content.Server._Ganimed.ImmersiveSpacing;

public sealed partial class SpacingImmersionSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SpacedAudioComponent, ComponentInit>(OnSpacing);
        SubscribeLocalEvent<SpacedAudioComponent, ComponentRemove>(OnUnSpacing);
    }
    private void OnSpacing(EntityUid uid, SpacedAudioComponent spacedAudio, ComponentInit args)
    {
        _popupSystem.PopupEntity("Pee pee", uid, uid);
        return;
    }

    private void OnUnSpacing(EntityUid uid, SpacedAudioComponent spacedAudio, ComponentRemove args)
    {
        _popupSystem.PopupEntity("Poo poo", uid, uid);
        return;
    }
}

