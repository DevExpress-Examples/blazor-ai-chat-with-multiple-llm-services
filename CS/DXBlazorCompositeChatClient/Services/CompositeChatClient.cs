using Microsoft.Extensions.AI;

namespace DXBlazorChatSelector.Services;

public class CompositeChatClient : IChatClient
{
    public List<ChatClientSession> AvailableChatClients { get; }
    public ChatClientSession? SelectedSession { get; set; }
    public CompositeChatClient(params ChatClientSession[] chatClients)
    {
        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return SelectedSession?.Client.GetResponseAsync(messages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken()) {
        return SelectedSession?.Client.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    public void Dispose() {
        for (int i = 0; i < AvailableChatClients.Count; i++)
        {
            AvailableChatClients[i].Client.Dispose();
            AvailableChatClients[i].Messages.Clear();
        }
    }
    public object? GetService(Type serviceType, object? serviceKey = null) {
        throw new NotImplementedException();
    }
}