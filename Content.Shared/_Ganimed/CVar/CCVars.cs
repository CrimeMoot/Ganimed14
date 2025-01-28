using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Ganimed.CCVar
{
    [CVarDefs]
    public sealed class GanimedCCVars : CVars
    {
        /*
         * Discord
         */
        public static readonly CVarDef<string> DiscordBanWebhook =
            CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);
    }
}