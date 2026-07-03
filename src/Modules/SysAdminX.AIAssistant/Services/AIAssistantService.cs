// -----------------------------------------------------------------------
// <copyright file="AIAssistantService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;

namespace SysAdminX.AIAssistant.Services;

/// <summary>
/// Implementation of <see cref="IAIAssistantService"/> using HttpClient for OpenAI-compatible endpoints.
/// </summary>
public class AIAssistantService : IAIAssistantService
{
    private readonly ILogger<AIAssistantService> _logger;
    private readonly HttpClient _httpClient;

    public AIAssistantService(ILogger<AIAssistantService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2); // Local LLMs can be slow
    }

    public async Task<Result<string>> SendMessageAsync(List<ChatMessageModel> chatHistory, AIProviderConfigModel config, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Sending prompt to AI Provider at {Endpoint}", config.EndpointUrl);

            var messages = new List<object>
            {
                new { role = "system", content = config.SystemPrompt }
            };

            messages.AddRange(chatHistory.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }));

            var payload = new
            {
                model = config.ModelName,
                messages = messages,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl)
            {
                Content = JsonContent.Create(payload)
            };

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
            }

            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("AI API Error: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return Result<string>.Failure($"API Error {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            
            // Parse OpenAI-compatible response format
            if (json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                {
                    return Result<string>.Success(content.GetString() ?? "");
                }
            }

            return Result<string>.Failure("Failed to parse the response from the AI provider.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error to AI provider");
            return Result<string>.Failure($"Connection failed. Is the local LLM running at {config.EndpointUrl}?");
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling AI provider");
            return Result<string>.Failure(ex.Message, ex);
        }
    }
}
