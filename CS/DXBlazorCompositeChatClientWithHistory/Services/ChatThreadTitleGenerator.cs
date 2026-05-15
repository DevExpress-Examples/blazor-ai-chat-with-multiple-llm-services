using System.Text;
using Microsoft.Extensions.AI;

namespace DXBlazorChatSelector.Services;

public class ChatThreadTitleGenerator : IChatThreadTitleGenerator
{
    private const int MaxWords = 6;
    private const string TitlePrompt = "Create a title for this chat. Return only title text. "
                                     + "Use 3 to 6 words. Use letters digits and spaces only. Do not use punctuation.";

    public async Task<string> GenerateTitleAsync(ChatClientSession modelSession, string firstUserMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelSession);

        if (string.IsNullOrWhiteSpace(firstUserMessage))
            throw new ArgumentException("First message cannot be empty.", nameof(firstUserMessage));

        var response = await modelSession.Client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, TitlePrompt),
                new ChatMessage(ChatRole.User, firstUserMessage)
            ],
            cancellationToken: cancellationToken);

        return Sanitize(response.Text, MaxWords);
    }

    public string BuildFallbackTitle(string firstUserMessage)
    {
        return string.IsNullOrWhiteSpace(firstUserMessage)
            ? "New Chat"
            : Sanitize(firstUserMessage, MaxWords);
    }

    private static string Sanitize(string? value, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ');

        var words = builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(maxWords)
            .ToArray();

        return string.Join(' ', words);
    }
}
