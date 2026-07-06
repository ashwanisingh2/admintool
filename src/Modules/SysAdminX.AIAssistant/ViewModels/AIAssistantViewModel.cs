// -----------------------------------------------------------------------
// <copyright file="AIAssistantViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.AIAssistant.Services;

namespace SysAdminX.AIAssistant.ViewModels;

/// <summary>
/// ViewModel for the AI Assistant chat interface.
/// </summary>
public partial class AIAssistantViewModel : ObservableObject
{
    private readonly ILogger<AIAssistantViewModel> _logger;
    private readonly IAIAssistantService _aiService;

    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _chatHistory = new();

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private AIProviderConfigModel _config = new() 
    { 
        EndpointUrl = "http://localhost:11434/v1/chat/completions", // Ollama OpenAI compatibility
        ModelName = "llama3"
    };

    public AIAssistantViewModel(ILogger<AIAssistantViewModel> logger, IAIAssistantService aiService)
    {
        _logger = logger;
        _aiService = aiService;
        
        // Initial greeting
        ChatHistory.Add(new ChatMessageModel 
        { 
            Role = "assistant", 
            Content = "Hello! I am your local IT Assistant. Ask me anything about troubleshooting, PowerShell, or network diagnostics."
        });
    }

    [RelayCommand]
    public async Task SendMessageAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(UserInput) || IsGenerating)
            return;

        var userText = UserInput;
        var userMessage = new ChatMessageModel { Role = "user", Content = userText };
        ChatHistory.Add(userMessage);

        UserInput = string.Empty;
        IsGenerating = true;

        // Per-request cancellation so the user can navigate away mid-response
        // without orphaning a hanging HTTP call.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(60));

        try
        {
            // Send a snapshot of the chat history so the model sees the full
            // context without us exposing internal mutation during the await.
            var snapshot = new System.Collections.Generic.List<ChatMessageModel>(ChatHistory);
            var result = await _aiService.SendMessageAsync(snapshot, Config, linkedCts.Token);

            if (result.IsSuccess)
            {
                ChatHistory.Add(new ChatMessageModel { Role = "assistant", Content = result.Value ?? "" });
            }
            else
            {
                var err = result.ErrorMessage ?? "Unknown error";
                _logger.LogWarning("AI request failed: {Error}", err);
                ChatHistory.Add(new ChatMessageModel
                {
                    Role = "assistant",
                    Content = $"⚠️ Error: {err}"
                });
            }
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            ChatHistory.Add(new ChatMessageModel
            {
                Role = "assistant",
                Content = "⚠️ Request timed out after 60 seconds. The local model may be busy — please try again."
            });
        }
        catch (OperationCanceledException)
        {
            // Caller-cancelled (page navigation / shutdown). Don't add a chat
            // message — the user is no longer looking.
            _logger.LogInformation("AI request cancelled by caller.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception sending message to AI service");
            ChatHistory.Add(new ChatMessageModel
            {
                Role = "assistant",
                Content = $"⚠️ Unexpected error: {ex.Message}"
            });
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void LoadTemplate(string templateName)
    {
        if (templateName == "LogAnalyzer")
        {
            UserInput = "Please analyze the following Windows Event Log entry and suggest a fix:\n\n[Paste Log Here]";
        }
        else if (templateName == "ScriptGenerator")
        {
            UserInput = "Write a PowerShell script to:\n1. \n2. \nEnsure it includes error handling and logging.";
        }
        else if (templateName == "ClearChat")
        {
            ChatHistory.Clear();
            ChatHistory.Add(new ChatMessageModel 
            { 
                Role = "assistant", 
                Content = "Chat history cleared. How can I help you today?"
            });
        }
    }
}
