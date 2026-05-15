using Microsoft.Extensions.AI;

namespace DXBlazorChatSelector.Services;

public class CompositeChatClient : IChatClient
{
    private readonly IChatThreadStore _threadStore;
    private readonly IChatThreadTitleGenerator _titleGenerator;
    private readonly object _syncRoot = new();
    private readonly HashSet<Guid> _titleGenerationInProgress = new();
    private readonly HashSet<Guid> _titledThreadIds = new();
    private Guid? _activeThreadId;
    private TaskCompletionSource? _titleGenerationGate;

    public List<ChatClientSession> AvailableChatClients { get; }
    public ChatClientSession? SelectedSession { get; set; }
    public event Action<Guid, string>? ThreadTitleUpdated;

    public CompositeChatClient(IChatThreadStore threadStore, IChatThreadTitleGenerator titleGenerator, params ChatClientSession[] chatClients)
    {
        _threadStore = threadStore ?? throw new ArgumentNullException(nameof(threadStore));
        _titleGenerator = titleGenerator ?? throw new ArgumentNullException(nameof(titleGenerator));

        if (chatClients is null || chatClients.Length == 0)
        {
            throw new ArgumentException("At least one chat client session must be provided.", nameof(chatClients));
        }

        AvailableChatClients = chatClients.ToList();
        SelectedSession = AvailableChatClients[0];
    }

    public ChatClientSession GetRequiredSelectedSession()
    {
        return SelectedSession ?? throw new InvalidOperationException("No model session is selected.");
    }

    public void SetActiveThread(Guid? threadId)
    {
        _activeThreadId = threadId;
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var selectedSession = GetRequiredSelectedSession();
        var messageList = messages.ToList();
        TryQueueTitleGeneration(messageList, selectedSession);
        var gate = _titleGenerationGate;
        try
        {
            var response = await selectedSession.Client.GetResponseAsync(messageList, options, cancellationToken);
            SignalTitleGenerationGate(gate, succeeded: true);
            return response;
        }
        catch
        {
            SignalTitleGenerationGate(gate, succeeded: false);
            throw;
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var selectedSession = GetRequiredSelectedSession();
        var messageList = messages.ToList();
        TryQueueTitleGeneration(messageList, selectedSession);
        var gate = _titleGenerationGate;

        return Iterator();

        async IAsyncEnumerable<ChatResponseUpdate> Iterator()
        {
            var streamCompleted = false;
            try
            {
                await foreach (var update in selectedSession.Client.GetStreamingResponseAsync(messageList, options, cancellationToken))
                    yield return update;
                streamCompleted = true;
            }
            finally
            {
                SignalTitleGenerationGate(gate, streamCompleted);
            }
        }
    }

    private static void SignalTitleGenerationGate(TaskCompletionSource? gate, bool succeeded)
    {
        if (succeeded)
            gate?.TrySetResult();
        else
            gate?.TrySetCanceled();
    }

    public void Dispose()
    {
        foreach (var session in AvailableChatClients)
        {
            session.Client.Dispose();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return GetRequiredSelectedSession().Client.GetService(serviceType, serviceKey);
    }

    private void TryQueueTitleGeneration(IEnumerable<ChatMessage> messages, ChatClientSession selectedSession)
    {
        if (_activeThreadId is null)
        {
            return;
        }

        var threadId = _activeThreadId.Value;
        var firstUserMessage = GetFirstUserMessage(messages);
        if (string.IsNullOrWhiteSpace(firstUserMessage))
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_titledThreadIds.Contains(threadId))
            {
                return;
            }

            if (_titleGenerationInProgress.Contains(threadId))
            {
                return;
            }

            _titleGenerationInProgress.Add(threadId);
        }

        _titleGenerationGate?.TrySetCanceled();
        _titleGenerationGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = GenerateTitleForThreadAsync(threadId, selectedSession, firstUserMessage);
    }

    private async Task GenerateTitleForThreadAsync(Guid threadId, ChatClientSession selectedSession, string firstUserMessage)
    {
        var gate = _titleGenerationGate;
        try
        {
            if (gate != null)
                await gate.Task;

            var thread = await _threadStore.GetThreadAsync(threadId, CancellationToken.None);
            if (thread is null || thread.HasGeneratedTitle)
            {
                return;
            }

            var modelSession = AvailableChatClients.FirstOrDefault(x => x.Id == thread.ModelSessionId) ?? selectedSession;

            string generatedTitle;
            try
            {
                generatedTitle = await _titleGenerator.GenerateTitleAsync(modelSession, firstUserMessage, CancellationToken.None);
            }
            catch
            {
                generatedTitle = _titleGenerator.BuildFallbackTitle(firstUserMessage);
            }

            if (string.IsNullOrWhiteSpace(generatedTitle))
            {
                generatedTitle = _titleGenerator.BuildFallbackTitle(firstUserMessage);
            }

            await _threadStore.UpdateTitleAsync(threadId, generatedTitle, true, CancellationToken.None);
            lock (_syncRoot)
            {
                _titledThreadIds.Add(threadId);
            }
            ThreadTitleUpdated?.Invoke(threadId, generatedTitle);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_syncRoot)
            {
                _titleGenerationInProgress.Remove(threadId);
            }
        }
    }

    private static string? GetFirstUserMessage(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))
            {
                return message.Text;
            }
        }

        return null;
    }
}
