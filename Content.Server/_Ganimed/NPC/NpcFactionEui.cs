using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared._Ganimed.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server._Ganimed.NPC;

public sealed class NpcFactionEui : BaseEui
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private readonly NpcFactionSystem _factionSystem;

    private ISawmill _sawmill = default!;
    private EntityUid _target;

    public NpcFactionEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _target = target;
        _factionSystem = _entitySystemManager.GetEntitySystem<NpcFactionSystem>();
        _sawmill = Logger.GetSawmill("npc-faction-eui");
    }

    public override EuiStateBase GetNewState()
    {
        _entityManager.TryGetComponent<NpcFactionMemberComponent>(_target, out var factions);
        return new NpcFactionEuiState(_entityManager.GetNetEntity(_target), factions?.Factions ?? []);
    }

    public override void Opened()
    {
        base.Opened();

        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (!IsAllowed())
            return;

        switch (msg)
        {
            case NpcFactionAddMessage message:
                _target = _entityManager.GetEntity(message.Target);

                if (_factionSystem.IsMember(_target, message.Faction))
                {
                    StateDirty();
                    return;
                }

                _factionSystem.AddFaction(_target, message.Faction);
                break;
            case NpcFactionRemoveMessage message:
                _target = _entityManager.GetEntity(message.Target);

                if (!_factionSystem.IsMember(_target, message.Faction))
                {
                    StateDirty();
                    return;
                }

                _factionSystem.RemoveFaction(_target, message.Faction);
                break;
            case NpcFactionCreateComponentMessage message:
                _target = _entityManager.GetEntity(message.Target);
                _entityManager.EnsureComponent<NpcFactionMemberComponent>(_target);
                break;
            default:
                return;
        }

        StateDirty();
    }

    private bool IsAllowed()
    {
        var adminData = _adminManager.GetAdminData(Player);
        if (adminData == null || !adminData.HasFlag(AdminFlags.Moderator))
        {
            _sawmill.Warning("Player {0} tried to open / use NPC faction UI without permission.", Player.UserId);
            return false;
        }

        return true;
    }
}