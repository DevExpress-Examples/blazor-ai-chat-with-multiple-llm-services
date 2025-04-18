using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;

namespace DXBlazorChatSelector.Services;

public class ChatClientSession
{
    public string Name { get; set; }
    public IChatClient Client { get; }
    public List<BlazorChatMessage> Messages { get; set; }

    public ChatClientSession(IChatClient client, string name)
    {
        Name = name;
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Messages = new List<BlazorChatMessage>();
    }
}