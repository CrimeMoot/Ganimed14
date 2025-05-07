using Content.Server.Discord;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Content.Shared.Database;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Database;

namespace Content.Server._Ganimed.Discord;

public sealed class DiscordWebhookBanSender
{
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] public IServerDbManager _db = default!;

    private ISawmill _sawmill = default!;

    private readonly string _thumbnailIconUrl = "https://static.wikia.nocookie.net/ss14andromeda13/images/f/ff/Clown.png/revision/latest?cb=20230217121049&path-prefix=ru";

    public DiscordWebhookBanSender()
    {
        _sawmill = Logger.GetSawmill("discord");
    }

    public async void SendBanMessage(string? targetUsername, NetUserId? targetUserId, string? banningAdmin, NetUserId? banningAdminId, uint minutes, string reason)
    {
        try
        {
            var gameTicker = _entSys.GetEntitySystemOrNull<GameTicker>();
            var serverName = _cfg.GetCVar(CCVars.GameHostName);

            var runId = gameTicker != null ? gameTicker.RoundId : 0;

            var expired = minutes != 0 ? $"{DateTimeOffset.Now + TimeSpan.FromMinutes(minutes)}" : "Никогда";

            var dataTargetUser = await _db.GetPlayerRecordByUserId((NetUserId)targetUserId!);

            var dataBanningAdmin = await _db.GetPlayerRecordByUserId((NetUserId)banningAdminId!);

            var payload = new WebhookPayload()
            {
                Username = "1984",
                Embeds = new List<WebhookEmbed>
                    {
                        new()
                        {
                            Title = minutes != 0 ? $"Временный бан на {minutes} мин (Раунд #{runId})" : $"Перманентная блокировка (Раунд #{runId})",
                            Color = minutes != 0 ? 16600397 : 13438992,
                            Description =  $"""
                                > **Администратор:** {banningAdmin ?? "Консоль"}

                                > **Нарушитель:** {targetUsername ?? "Неизвестно"}
                                                     
                                > **Выдан:** {DateTimeOffset.Now}
                                > **Истекает:** {expired}

                                > **Причина:** {reason}
                                """,
                            Footer = new WebhookEmbedFooter
                            {
                                Text = serverName,
                            },
                            Thumbnail = new WebhookEmbedThumbnail
                            {
                                Url = _thumbnailIconUrl
                            }
                        },
                    },
            };

            var webhookUrl = _cfg.GetCVar(CCVars.DiscordBanWebhook);

            if (string.IsNullOrEmpty(webhookUrl))
                return;

            if (await _discord.GetWebhook(webhookUrl) is not { } identifier)
                return;

            var request = await _discord.CreateMessage(identifier.ToIdentifier(), payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending ban webhook to Discord: {e}");
        }
    }

    public async void SendRoleBansMessage(string? targetUsername, NetUserId? targetUserId, string? banningAdmin, NetUserId? banningAdminId, uint minutes, string reason, IReadOnlyCollection<string>? roles)
    {
        try
        {
            var gameTicker = _entSys.GetEntitySystemOrNull<GameTicker>();
            var serverName = _cfg.GetCVar(CCVars.GameHostName);

            var runId = gameTicker != null ? gameTicker.RoundId : 0;

            var expired = minutes != 0 ? $"{DateTimeOffset.Now + TimeSpan.FromMinutes(minutes)}" : "Никогда";

            var dataTargetUser = await _db.GetPlayerRecordByUserId((NetUserId)targetUserId!);

            var dataBanningAdmin = await _db.GetPlayerRecordByUserId((NetUserId)banningAdminId!);

            string formattedRolesStr = "";

            foreach (var role in roles!)
            {
                formattedRolesStr = formattedRolesStr + $"\n> `{Loc.GetString($"Job{role}")}`";
            }

            var payload = new WebhookPayload()
            {
                Username = "1984",
                Embeds = new List<WebhookEmbed>
                    {
                        new()
                        {
                            Title = minutes != 0 ? $"Временный джоб-бан на {minutes} мин (Раунд #{runId})" : $"Перманентный джоб-бан (Раунд #{runId})",
                            Color = minutes != 0 ? 28927 : 2815,
                            Description =  $"""
                                > **Администратор:** {banningAdmin ?? "Консоль"}

                                > **Нарушитель:** {targetUsername ?? "Неизвестно"}

                                > **Выдан:** {DateTimeOffset.Now}
                                > **Истекает:** {expired}

                                > **Роли:** {formattedRolesStr}

                                > **Причина:** {reason}
                                """,
                            Footer = new WebhookEmbedFooter
                            {
                                Text = serverName,
                            },
                            Thumbnail = new WebhookEmbedThumbnail
                            {
                                Url = _thumbnailIconUrl
                            }
                        },
                    },
            };

            var webhookUrl = _cfg.GetCVar(CCVars.DiscordBanWebhook);

            if (string.IsNullOrEmpty(webhookUrl))
                return;

            if (await _discord.GetWebhook(webhookUrl) is not { } identifier)
                return;

            var request = await _discord.CreateMessage(identifier.ToIdentifier(), payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending ban webhook to Discord: {e}");
        }
    }
}