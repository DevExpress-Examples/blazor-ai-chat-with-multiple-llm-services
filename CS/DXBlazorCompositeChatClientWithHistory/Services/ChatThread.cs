using DevExpress.AIIntegration.Blazor.Chat;

namespace DXBlazorChatSelector.Services;

public class ChatThread
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "New Chat";
    public string ModelSessionId { get; set; } = string.Empty;
    public bool HasGeneratedTitle { get; set; }
    public List<BlazorChatMessage> Messages { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

