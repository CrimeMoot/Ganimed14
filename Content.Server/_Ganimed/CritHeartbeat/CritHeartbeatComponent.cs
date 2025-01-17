using Robust.Shared.Audio;

namespace Content.Server._Ganimed.CritHeartbeat;

[RegisterComponent]
public sealed partial class CritHeartbeatComponent : Component
{
    [DataField]
    public SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Ganimed/Effects/heartbeat.ogg");

    /// <summary>
    /// Чтобы выключать это для наследников в прототипах
    /// </summary>
    [DataField]
    public bool Enabled = true;

    public EntityUid? AudioStream;
}