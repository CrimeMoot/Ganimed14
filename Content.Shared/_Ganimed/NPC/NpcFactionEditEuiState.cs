using Content.Shared.Eui;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Ganimed.NPC;

[Serializable, NetSerializable]
public sealed class NpcFactionEuiState : EuiStateBase
{
    public NetEntity Target { get; }
    public HashSet<ProtoId<NpcFactionPrototype>> Factions { get; }

    public NpcFactionEuiState(NetEntity target, HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        Target = target;
        Factions = factions;
    }
}

[Serializable, NetSerializable]
public sealed class NpcFactionCreateComponentMessage : EuiMessageBase
{
    public NetEntity Target { get; }

    public NpcFactionCreateComponentMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class NpcFactionAddMessage : EuiMessageBase
{
    public NetEntity Target { get; }
    public ProtoId<NpcFactionPrototype> Faction { get; }

    public NpcFactionAddMessage(NetEntity target, ProtoId<NpcFactionPrototype> faction)
    {
        Target = target;
        Faction = faction;
    }
}

[Serializable, NetSerializable]
public sealed class NpcFactionRemoveMessage : EuiMessageBase
{
    public NetEntity Target { get; }
    public ProtoId<NpcFactionPrototype> Faction { get; }

    public NpcFactionRemoveMessage(NetEntity target, ProtoId<NpcFactionPrototype> faction)
    {
        Target = target;
        Faction = faction;
    }
}