using Content.Shared.Actions;
using Content.Shared.Mech.Components;
using Content.Shared.Mind;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems; // Ganimed edit
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events; // Ganimed edit
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode;

public abstract class SharedCombatModeSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly SharedMindSystem  _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CombatModeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
        SubscribeLocalEvent<CombatModeComponent, AttemptMobCollideEvent>(OnCollide); // Ganimed edit
    }

    // Ganimed edit start
    private void OnCollide(EntityUid uid, CombatModeComponent component, ref AttemptMobCollideEvent args)
    {
        if (!component.IsInCombatMode)
        {
            args.Cancelled = true;
        }
    }
    // Ganimed edit end

    private void OnMapInit(EntityUid uid, CombatModeComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.CombatToggleActionEntity, component.CombatToggleAction);
        Dirty(uid, component);
    }

    private void OnShutdown(EntityUid uid, CombatModeComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.CombatToggleActionEntity);

        SetMouseRotatorComponents(uid, false);
    }

    private void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetInCombatMode(uid, !component.IsInCombatMode, component);

        var msg = component.IsInCombatMode ? "action-popup-combat-enabled" : "action-popup-combat-disabled";
        _popup.PopupClient(Loc.GetString(msg), args.Performer, args.Performer);
    }

    public void SetCanDisarm(EntityUid entity, bool canDisarm, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        component.CanDisarm = canDisarm;
    }

    public bool IsInCombatMode(EntityUid? entity, CombatModeComponent? component = null)
    {
        return entity != null && Resolve(entity.Value, ref component, false) && component.IsInCombatMode;
    }

    public virtual void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return;

        if (component.IsInCombatMode == value)
            return;

        component.IsInCombatMode = value;
        Dirty(entity, component);

        if (component.CombatToggleActionEntity != null)
            _actionsSystem.SetToggled(component.CombatToggleActionEntity, component.IsInCombatMode);

        // Change mouse rotator comps if flag is set
        if (!component.ToggleMouseRotator || IsNpc(entity) && !_mind.TryGetMind(entity, out _, out _))
            return;

        SetMouseRotatorComponents(entity, value);
    }

    private void SetMouseRotatorComponents(EntityUid uid, bool value)
    {
        if (value)
        {
            // Ganimed edit start
            var rot = EnsureComp<MouseRotatorComponent>(uid);
            if (TryComp<CombatModeComponent>(uid, out var comp) && comp.SmoothRotation) // no idea under which (intended) circumstances this can fail (if any), so i'll avoid Comp<>().
            {
                rot.AngleTolerance = Angle.FromDegrees(1); // arbitrary
                rot.Simple4DirMode = false;
            }
            // Ganimed edit end
            EnsureComp<NoRotateOnMoveComponent>(uid);

            // ADT Mech start
            if (TryComp<MechPilotComponent>(uid, out var pilot))
                EnsureComp<NoRotateOnMoveComponent>(pilot.Mech);
            // ADT Mech end
        }
        else
        {
            RemComp<MouseRotatorComponent>(uid);
            RemComp<NoRotateOnMoveComponent>(uid);

            // ADT Mech start
            if (TryComp<MechPilotComponent>(uid, out var pilot))
                RemComp<NoRotateOnMoveComponent>(pilot.Mech);
            // ADT Mech end
        }
    }

    // todo: When we stop making fucking garbage abstract shared components, remove this shit too.
    protected abstract bool IsNpc(EntityUid uid);
}

public sealed partial class ToggleCombatActionEvent : InstantActionEvent
{

}
