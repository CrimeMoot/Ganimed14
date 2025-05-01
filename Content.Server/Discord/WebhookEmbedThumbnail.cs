using System.Text.Json.Serialization;

namespace Content.Server.Discord;

public struct WebhookEmbedThumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    public WebhookEmbedThumbnail()
    {
    }
}