using Azure;
using Azure.AI.OpenAI;
using DXBlazorChatSelector.Components;
using DXBlazorChatSelector.Services;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDevExpressBlazor();
builder.Services.AddMvc();

// Configure your Azure OpenAI and Ollama settings in appsettings.Development.json
// or use .NET User Secrets: https://learn.microsoft.com/aspnet/core/security/app-secrets
var openAiServiceSettings = builder.Configuration.GetSection("OpenAISettings").Get<OpenAIServiceSettings>();
var ollamaSettings = builder.Configuration.GetSection("OllamaSettings").Get<OllamaSettings>();

// Register individual IChatClient instances as keyed scoped services
builder.Services.AddKeyedScoped<IChatClient>("azure-openai", (_, _) =>
    new AzureOpenAIClient(
            new Uri(openAiServiceSettings.Endpoint),
            new AzureKeyCredential(openAiServiceSettings.Key))
        .GetChatClient(openAiServiceSettings.DeploymentName)
        .AsIChatClient());

builder.Services.AddKeyedScoped<IChatClient>("ollama-phi4", (_, _) =>
    new OllamaChatClient(
        new Uri(ollamaSettings.Uri),
        ollamaSettings.ModelName,
        new HttpClient { Timeout = TimeSpan.FromMinutes(10) }));

// Assemble the composite client from keyed IChatClient services
builder.Services.AddScoped<CompositeChatClient>(provider => {
    var threadStore = provider.GetRequiredService<IChatThreadStore>();
    var titleGenerator = provider.GetRequiredService<IChatThreadTitleGenerator>();

    return new CompositeChatClient(
        threadStore,
        titleGenerator,
        new ChatClientSession(
            provider.GetRequiredKeyedService<IChatClient>("azure-openai"),
            "azure-openai",
            $"Azure Open AI - {openAiServiceSettings.DeploymentName}"),
        new ChatClientSession(
            provider.GetRequiredKeyedService<IChatClient>("ollama-phi4"),
            "ollama-phi4",
            $"Ollama - {ollamaSettings.ModelName}"));
});
builder.Services.AddScoped<IChatClient>(provider => provider.GetRequiredService<CompositeChatClient>());
builder.Services.AddSingleton<IChatThreadStore, InMemoryChatThreadStore>();
builder.Services.AddScoped<IChatThreadTitleGenerator, ChatThreadTitleGenerator>();
builder.Services.AddDevExpressAI();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();