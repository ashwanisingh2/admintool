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

        var userMessage = new ChatMessageModel { Role = "user", Content = UserInput };
        ChatHistory.Add(userMessage);
        
        UserInput = string.Empty;
        IsGenerating = true;

        var result = await _aiService.SendMessageAsync(new System.Collections.Generic.List<ChatMessageModel>(ChatHistory), Config, ct);

        if (result.IsSuccess)
        {
            ChatHistory.Add(new ChatMessageModel { Role = "assistant", Content = result.Value ?? "" });
        }
        else
        {
            ChatHistory.Add(new ChatMessageModel { Role = "assistant", Content = $"⚠️ Error: {result.ErrorMessage}" });
        }

        IsGenerating = false;
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
