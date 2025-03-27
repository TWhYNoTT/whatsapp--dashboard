using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;

namespace Labys.Application.Services.Interfaces
{
    /// <summary>
    /// Chatbot service interface
    /// </summary>
    public interface IChatbotService
    {
        Task<ApiResponse<ChatbotResponseDTO>> ProcessIncomingMessageAsync(string message, string phoneNumber, int? customerId = null);
        Task<ApiResponse<bool>> QueueHumanFollowUpAsync(string phoneNumber, string message, int? customerId = null);
        Task<ApiResponse<IEnumerable<ChatbotRule>>> GetRulesAsync(bool activeOnly = false);
        Task<ApiResponse<ChatbotRule>> GetRuleAsync(int id);
        Task<ApiResponse<ChatbotRule>> CreateRuleAsync(ChatbotRule rule);
        Task<ApiResponse<ChatbotRule>> UpdateRuleAsync(int id, ChatbotRule rule);
        Task<ApiResponse<bool>> DeleteRuleAsync(int id);
        Task<ApiResponse<bool>> ToggleRuleStatusAsync(int id);
        Task<ApiResponse<ChatbotResponseDTO>> TestRuleAsync(string message, int? customerId = null);
    }
}