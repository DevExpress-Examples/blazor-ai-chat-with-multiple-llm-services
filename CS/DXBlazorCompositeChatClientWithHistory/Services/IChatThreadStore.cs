using DevExpress.AIIntegration.Blazor.Chat;

namespace DXBlazorChatSelector.Services;

public interface IChatThreadStore
{
    Task<IReadOnlyList<ChatThread>> GetThreadsAsync(CancellationToken cancellationToken = default);
    Task<ChatThread?> GetThreadAsync(Guid threadId, CancellationToken cancellationToken = default);
    Task<ChatThread> CreateThreadAsync(string modelSessionId, CancellationToken cancellationToken = default);
    Task SaveMessagesAsync(Guid threadId, IEnumerable<BlazorChatMessage> messages, CancellationToken cancellationToken = default);
    Task UpdateTitleAsync(Guid threadId, string title, bool hasGeneratedTitle, CancellationToken cancellationToken = default);
    Task UpdateModelSessionAsync(Guid threadId, string modelSessionId, CancellationToken cancellationToken = default);
}
