using Content.Shared._Cats.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class LogProbeUiState : BoundUserInterfaceState
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    public List<PulledAccessLog> PulledLogs;

    /// <summary>
    /// Cats-PDAChat: The NanoChat data if a card was scanned, null otherwise
    /// </summary>
    public NanoChatData? NanoChatData { get; }

    public LogProbeUiState(List<PulledAccessLog> pulledLogs, NanoChatData? nanoChatData = null) // Cats-PDAChat - NanoChat support
    {
        PulledLogs = pulledLogs;
                NanoChatData = nanoChatData; // Cats-PDAChat
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class PulledAccessLog
{
    public readonly TimeSpan Time;
    public readonly string Accessor;

    public PulledAccessLog(TimeSpan time, string accessor)
    {
        Time = time;
        Accessor = accessor;
    }
}
