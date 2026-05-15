using Microsoft.Extensions.AI;

namespace DXBlazorChatSelector.Services;

public class ChatClientSession
{
    public string Id { get; }
    public string Name { get; set; }
    public IChatClient Client { get; }

    public ChatClientSession(IChatClient client, string id, string name)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Session id cannot be empty.", nameof(id)) : id;
        Name = name;
        Client = client ?? throw new ArgumentNullException(nameof(client));
    }
}