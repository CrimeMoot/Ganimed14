using Content.Server.Access;
using Content.Server.Forensics; // Ganimed edit
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Power;
using Robust.Shared.Physics.Components;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!; /// <summary>
    /// Initializes the DoorSystem and registers its event handlers.
    /// </summary>
    /// <remarks>
    /// Calls the base initialization and subscribes a local handler to receive PowerChangedEvent for DoorBoltComponent,
    /// enabling the system to react when door bolt power changes.
    /// </remarks>

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, PowerChangedEvent>(OnBoltPowerChanged);
    }

    protected override void SetCollidable(
        EntityUid uid,
        bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.ChangeAirtight && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked((uid, airtight), collidable);

        // Pathfinding / AI stuff.
        RaiseLocalEvent(new AccessReaderChangeEvent(uid, collidable));

        base.SetCollidable(uid, collidable, door, physics, occluder);
    }

    /// <summary>
    /// Handles changes to the bolt's power state.
    /// </summary>
    /// <remarks>
    /// If the bolt becomes powered and its wire has been cut, this will force the bolts down.
    /// Always updates the component's <c>Powered</c> field, marks the component dirty, and refreshes the bolt light status.
    /// </remarks>
    /// <param name="ent">Entity wrapper containing the <see cref="DoorBoltComponent"/> whose power changed.</param>
    /// <param name="args">Event args describing the new power state.</param>
    private void OnBoltPowerChanged(Entity<DoorBoltComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (ent.Comp.BoltWireCut)
                SetBoltsDown(ent, true);
        }

        ent.Comp.Powered = args.Powered;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);
    }

    /// <summary>
    /// Begins opening the door and records forensic evidence if a user is provided.
    /// </summary>
    /// <param name="user">Optional entity that initiated the open action; when present, forensic evidence is applied to the door for that user.</param>
    /// <param name="predicted">If true, the action is a client-side prediction and may be treated differently by callers.</param>
    public override void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        base.StartOpening(uid, door, user, predicted);

        if (user.HasValue)
            _forensicsSystem.ApplyEvidence(user.Value, uid);
    }

    /// <summary>
    /// Initiates closing of a door (overrides base behavior) and records forensic evidence if a user performed the action.
    /// </summary>
    /// <param name="user">Optional entity id of the user who initiated the close; when present, forensic evidence will be applied to the door.</param>
    /// <param name="predicted">Whether the close was predicted (client-side) rather than confirmed by the server.</param>
    public override void StartClosing(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        base.StartClosing(uid, door, user, predicted);

        if (user.HasValue)
            _forensicsSystem.ApplyEvidence(user.Value, uid);
    }
    // Ganimed edit end
}
