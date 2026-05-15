namespace DXBlazorChatSelector.Services;

public interface IChatThreadTitleGenerator
{
    Task<string> GenerateTitleAsync(ChatClientSession modelSession, string firstUserMessage, CancellationToken cancellationToken = default);
    string BuildFallbackTitle(string firstUserMessage);
}

