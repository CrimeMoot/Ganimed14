using System.Diagnostics.CodeAnalysis;
using Content.Shared.Corvax.Sponsors;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Разрешает выбирать лодаут только спонсорам. 
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (session == null)
            return true;

        var net = collection.Resolve<INetManager>();
        var isSponsor = false;

        if (net.IsClient)
        {
            if (collection.TryResolveType<ISponsorsManager>(out var sponsorsClient))
                isSponsor = sponsorsClient.TryGetInfo(out var info) && info != null;
        }
        else
        {
            if (collection.TryResolveType<ISponsorsManager>(out var sponsorsServer))
                isSponsor = sponsorsServer.TryGetInfo(session.UserId, out var info) && info != null;
        }

        if (!isSponsor)
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
            return false;
        }

        return true;
    }
}