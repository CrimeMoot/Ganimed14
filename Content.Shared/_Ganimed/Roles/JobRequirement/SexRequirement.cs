using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Roles
{
    /// <summary>
    /// Requires the character to be of a specified sex.
    /// </summary>
    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class SexRequirement : JobRequirement
    {
        [DataField(required: true)]
        public HashSet<Sex> AllowedSex = new();

        /// <summary>
        /// Checks if the character matches the required gender.
        /// </summary>
        /// <param name="entManager">Entity manager</param>
        /// <param name="protoManager">Prototype manager</param>
        /// <param name="profile">Character profile</param>
        /// <param name="playTimes">Playtime</param>
        /// <param name="reason">Rejection reason if the check fails</param>
        /// <returns>Returns true if the check is passed, otherwise false</returns>

        public override bool Check(IEntityManager entManager,
            IPrototypeManager protoManager,
            HumanoidCharacterProfile? profile,
            IReadOnlyDictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out FormattedMessage? reason)
        {
            reason = new FormattedMessage();

            if (profile is null)
                return true;

            var sb = new StringBuilder();
            sb.Append("[color=yellow]");

            sb.Append(string.Join(", ", AllowedSex));

            sb.Append("[/color]");

            if (!Inverted)
            {
                reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-whitelisted-sex")}\n{sb}");

                if (!AllowedSex.Contains(profile.Sex))
                    return false;
            }

            return true;
        }
    }
}