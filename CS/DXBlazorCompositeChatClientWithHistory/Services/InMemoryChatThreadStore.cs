using DevExpress.AIIntegration.Blazor.Chat;

namespace DXBlazorChatSelector.Services;

public class InMemoryChatThreadStore : IChatThreadStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, ChatThread> _threads = new();

    public Task<IReadOnlyList<ChatThread>> GetThreadsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var result = _threads.Values
                .OrderByDescending(thread => thread.UpdatedUtc)
                .Select(CloneThread)
                .ToList();
            return Task.FromResult<IReadOnlyList<ChatThread>>(result);
        }
    }

    public Task<ChatThread?> GetThreadAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var hasValue = _threads.TryGetValue(threadId, out var thread);
            return Task.FromResult(hasValue ? CloneThread(thread!) : null);
        }
    }

    public Task<ChatThread> CreateThreadAsync(string modelSessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(modelSessionId))
        {
            throw new ArgumentException("Model session id cannot be empty.", nameof(modelSessionId));
        }

        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var thread = new ChatThread
            {
                Id = Guid.NewGuid(),
                Title = "New Chat",
                ModelSessionId = modelSessionId,
                HasGeneratedTitle = false,
                Messages = new List<BlazorChatMessage>(),
                CreatedUtc = now,
                UpdatedUtc = now
            };
            _threads[thread.Id] = thread;
            return Task.FromResult(CloneThread(thread));
        }
    }

    public Task SaveMessagesAsync(Guid threadId, IEnumerable<BlazorChatMessage> messages, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_threads.TryGetValue(threadId, out var thread))
            {
                throw new InvalidOperationException($"Thread '{threadId}' was not found.");
            }

            thread.Messages = CloneMessages(messages);
            thread.UpdatedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    public Task UpdateTitleAsync(Guid threadId, string title, bool hasGeneratedTitle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_threads.TryGetValue(threadId, out var thread))
            {
                throw new InvalidOperationException($"Thread '{threadId}' was not found.");
            }

            thread.Title = title;
            thread.HasGeneratedTitle = hasGeneratedTitle;
            thread.UpdatedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    public Task UpdateModelSessionAsync(Guid threadId, string modelSessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_threads.TryGetValue(threadId, out var thread))
            {
                throw new InvalidOperationException($"Thread '{threadId}' was not found.");
            }

            thread.ModelSessionId = modelSessionId;
            thread.UpdatedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    private static ChatThread CloneThread(ChatThread source)
    {
        return new ChatThread
        {
            Id = source.Id,
            Title = source.Title,
            ModelSessionId = source.ModelSessionId,
            HasGeneratedTitle = source.HasGeneratedTitle,
            Messages = CloneMessages(source.Messages),
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc
        };
    }

    private static List<BlazorChatMessage> CloneMessages(IEnumerable<BlazorChatMessage> messages)
    {
        return messages.ToList();
    }
}


