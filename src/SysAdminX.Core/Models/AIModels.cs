// -----------------------------------------------------------------------
// <copyright file="AIModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a chat message in the AI Assistant.
/// </summary>
public record ChatMessageModel
{
    public string Role { get; init; } = string.Empty; // "user" or "assistant" or "system"
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;
    
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
}

/// <summary>
/// Configuration for the local AI provider.
/// </summary>
public record AIProviderConfigModel
{
    public string EndpointUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ModelName { get; set; } = "gpt-4o-mini";
    public string SystemPrompt { get; set; } = "You are an expert IT systems administrator assistant. Provide clear, concise, and safe PowerShell scripts or troubleshooting steps.";
    public string ApiKey { get; set; } = string.Empty;
}
