using Content.Client.Eui;
using Content.Client._Ganimed.NPC.UI;
using Content.Shared._Ganimed.NPC;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;
using System.Linq;

namespace Content.Client._Ganimed.NPC;

public sealed class NpcFactionEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private NpcFactionEditWindow? _window = null;
    private EntityUid? _target = null;
    private HashSet<ProtoId<NpcFactionPrototype>>? _factions = null;

    public NpcFactionEui()
    {
        IoCManager.InjectDependencies(this);
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Opened()
    {
        _window = new NpcFactionEditWindow();
        _window.OnSelectFaction += (faction) =>
        {
            if (_target is not { } target)
                return;

            SendMessage(new NpcFactionAddMessage(_entities.GetNetEntity(target), faction));
        };
        _window.OnUnselectFaction += (faction) =>
        {
            if (_target is not { } target)
                return;

            SendMessage(new NpcFactionRemoveMessage(_entities.GetNetEntity(target), faction));
        };

        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not NpcFactionEuiState factionState)
            return;

        _target = _entities.GetEntity(factionState.Target);
        _factions = factionState.Factions;

        if (_window != null && _window.IsOpen)
            UpdateUI();
    }

    private void UpdateUI()
    {
        if (_window is not { } window)
            return;
        if (_target is not { } target)
            return;

        SetNpcName(window, target);
        RefreshFactions(window);
    }

    private void SetNpcName(NpcFactionEditWindow window, EntityUid target)
    {
        var metadata = _entities.GetComponent<MetaDataComponent>(target);
        window.SetNpcName(metadata.EntityName);
    }

    private void RefreshFactions(NpcFactionEditWindow window)
    {
        if (_factions is not { } currentFactions)
            return;

        var factions = _prototypes.EnumeratePrototypes<NpcFactionPrototype>().Select(proto => new ProtoId<NpcFactionPrototype>(proto.ID)).ToHashSet();

        window.SetFactions(factions, currentFactions);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        UpdateUI();
    }
}