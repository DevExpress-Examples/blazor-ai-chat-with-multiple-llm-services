using Azure;
using Azure.AI.OpenAI;
using DXBlazorChatSelector.Components;
using DXBlazorChatSelector.Services;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDevExpressBlazor(options => { options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5; });
builder.Services.AddMvc();

// Replace with your endpoint, API key, and deployed AI model name
var openAiServiceSettings = builder.Configuration.GetSection("OpenAISettings").Get<OpenAIServiceSettings>();
var ollamaSettings = builder.Configuration.GetSection("OllamaSettings").Get<OllamaSettings>();

var azureChatClient = new AzureOpenAIClient(
     new Uri(openAiServiceSettings.Endpoint),
     new AzureKeyCredential(openAiServiceSettings.Key)).AsChatClient(openAiServiceSettings.DeploymentName);
var ollamaChatClient = new OllamaChatClient(
    new Uri(ollamaSettings.Uri), 
    ollamaSettings.ModelName);

// Register both clients within a single instance of the IChatClient
var compositeChatClient = new CompositeChatClient(
    new ChatClientSession(azureChatClient, "Azure Open AI — GPT4o"), 
    new ChatClientSession(ollamaChatClient, "Ollama — Phi 4"));

builder.Services.AddScoped<IChatClient>((provider) => compositeChatClient);
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