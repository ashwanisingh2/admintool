// -----------------------------------------------------------------------
// <copyright file="IAIAssistantService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.AIAssistant.Services;

/// <summary>
/// Service for communicating with a local LLM via REST API.
/// </summary>
public interface IAIAssistantService
{
    Task<Result<string>> SendMessageAsync(List<ChatMessageModel> chatHistory, AIProviderConfigModel config, CancellationToken ct = default);
}
