
using System.Text.Json.Serialization;
namespace WeoGpt.Function;

public class DirectLineTokenResponse
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}