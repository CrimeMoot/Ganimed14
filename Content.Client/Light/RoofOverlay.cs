using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedRoofSystem _roof = default!;
    private readonly SharedTransformSystem _xformSystem;

    private List<Entity<MapGridComponent>> _grids = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public const int ContentZIndex = BeforeLightTargetOverlay.ContentZIndex + 1;

    public RoofOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _roof = _entManager.System<SharedRoofSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();

        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var mapEnt = _mapSystem.GetMap(args.MapId);

        if (!_entManager.TryGetComponent(mapEnt, out RoofComponent? roofComp) ||
            !_entManager.TryGetComponent(mapEnt, out MapGridComponent? grid))
        {
            return;
        }

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;

        var worldHandle = args.WorldHandle;
        var lightoverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = lightoverlay.EnlargedBounds;
        var target = lightoverlay.EnlargedLightTarget;

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                var invMatrix = target.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);

                var gridMatrix = _xformSystem.GetWorldMatrix(mapEnt);
                var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                worldHandle.SetTransform(matty);

                var tileEnumerator = _mapSystem.GetTilesEnumerator(mapEnt, grid, bounds);

                // Due to stencilling we essentially draw on unrooved tiles
                while (tileEnumerator.MoveNext(out var tileRef))
                {
                    if (!_entManager.TryGetComponent(grid.Owner, out RoofComponent? roof))
                        continue;

                    var invMatrix = target.GetWorldToLocalMatrix(eye, scale);

                    var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    var tileEnumerator = _mapSystem.GetTilesEnumerator(grid.Owner, grid, bounds);
                    var roofEnt = (grid.Owner, grid.Comp, roof);

                    // Due to stencilling we essentially draw on unrooved tiles
                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        if (!_roof.IsRooved(roofEnt, tileRef.GridIndices))
                        {
                            continue;
                        }

                        if (!found)
                            continue;
                    }

                    var local = _lookup.GetLocalBounds(tileRef, grid.TileSize);
                    worldHandle.DrawRect(local, roofComp.Color);
                }

            }, null);
    }
}
