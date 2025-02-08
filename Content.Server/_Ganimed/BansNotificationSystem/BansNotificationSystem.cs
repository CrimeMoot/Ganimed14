using Content.Shared._Ganimed.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared;
using Robust.Shared.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace Content.Server._Ganimed.BansNotifications
{

    public sealed class BansNotificationsSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _config = default!;
        private ISawmill _sawmill = default!;
        private readonly HttpClient _httpClient = new();
        private string _webhookUrl = String.Empty;
        private string _serverName = String.Empty;

        public override void Initialize()
        {
            _sawmill = Logger.GetSawmill("bans_notifications");
            SubscribeLocalEvent<BanEvent>(OnBan);
            SubscribeLocalEvent<JobBanEvent>(OnJobBan);
            SubscribeLocalEvent<DepartmentBanEvent>(OnDepartmentBan);
            _config.OnValueChanged(GanimedCCVars.DiscordBanWebhook, value => _webhookUrl = value, true);
            _config.OnValueChanged(CVars.GameHostName, value => _serverName = value, true);
        }

        private async void SendDiscordMessage(WebhookPayload payload)
        {
            var request = await _httpClient.PostAsync(_webhookUrl,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            _sawmill.Debug($"Discord webhook json: {JsonSerializer.Serialize(payload)}");

            var content = await request.Content.ReadAsStringAsync();
            if (!request.IsSuccessStatusCode)
            {
                _sawmill.Error($"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
                return;
            }
        }

        public void OnBan(BanEvent e)
        {
            if (String.IsNullOrEmpty(_webhookUrl))
                return;

            var expires = e.Expires == null ? Loc.GetString("discord-permanent") : Loc.GetString("discord-expires-at", ("date", e.Expires));
            var message = Loc.GetString("discord-ban-msg",
                ("username", e.Username),
                ("expires", expires),
                ("reason", e.Reason));

            var color = e.Severity switch
            {
                NoteSeverity.None => 0x6aa84f,
                NoteSeverity.Minor => 0x45818e,
                NoteSeverity.Medium => 0xf1c232,
                NoteSeverity.High => 0xff0000,
                _ => 0xff0000,
            };

            var payload = new WebhookPayload
            {

                Username = _serverName,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = message,
                        Color = color,
                        Footer = new EmbedFooter
                        {
                            Text = $"{e.AdminUsername}",
                        },
                    },
                },
            };

            SendDiscordMessage(payload);
        }

        public void OnJobBan(JobBanEvent e)
        {
            if (String.IsNullOrEmpty(_webhookUrl))
                return;

            var expires = e.Expires == null ? Loc.GetString("discord-permanent") : Loc.GetString("discord-expires-at", ("date", e.Expires));
            var message = Loc.GetString("discord-jobban-msg",
                ("username", e.Username),
                ("role", e.Job.LocalizedName),
                ("expires", expires),
                ("reason", e.Reason));


            var color = e.Severity switch
            {
                NoteSeverity.None => 0x6aa84f,
                NoteSeverity.Minor => 0x45818e,
                NoteSeverity.Medium => 0xf1c232,
                NoteSeverity.High => 0xff0000,
                _ => 0xff0000,
            };

            var payload = new WebhookPayload
            {
                Username = _serverName,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = message,
                        Color = color,
                        Footer = new EmbedFooter
                        {
                            Text = $"{e.AdminUsername}",
                        },
                    },
                },
            };

            SendDiscordMessage(payload);
        }

        public void OnDepartmentBan(DepartmentBanEvent e)
        {
            if (String.IsNullOrEmpty(_webhookUrl))
                return;
        }

        private struct WebhookPayload
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = "";

            [JsonPropertyName("avatar_url")]
            public string? AvatarUrl { get; set; } = "";

            [JsonPropertyName("embeds")]
            public List<Embed>? Embeds { get; set; } = null;

            [JsonPropertyName("allowed_mentions")]
            public Dictionary<string, string[]> AllowedMentions { get; set; } =
                new()
                {
                    { "parse", Array.Empty<string>() },
                };

            public WebhookPayload()
            {
            }
        }

        // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
        private struct Embed
        {
            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [JsonPropertyName("color")]
            public int Color { get; set; } = 0;

            [JsonPropertyName("footer")]
            public EmbedFooter? Footer { get; set; } = null;

            public Embed()
            {
            }
        }

        // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
        private struct EmbedFooter
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            [JsonPropertyName("icon_url")]
            public string? IconUrl { get; set; }

            public EmbedFooter()
            {
            }
        }

        // https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
        private struct WebhookData
        {
            [JsonPropertyName("guild_id")]
            public string? GuildId { get; set; } = null;

            [JsonPropertyName("channel_id")]
            public string? ChannelId { get; set; } = null;

            public WebhookData()
            {
            }
        }
    }
}