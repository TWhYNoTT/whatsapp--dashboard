using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;

namespace Labys.Application.Services.Interfaces
{
    /// <summary>
    /// WhatsApp service interface
    /// </summary>
    public interface IWhatsAppService
    {
        Task<ApiResponse<MessageResponseDTO>> SendMessageAsync(WhatsAppMessageDTO messageDto, string userId);
        Task<ApiResponse<MessageResponseDTO>> SendTemplateMessageAsync(WhatsAppTemplateDTO templateDto, string userId);
        Task<ApiResponse<MessageResponseDTO>> SendMediaMessageAsync(WhatsAppMediaMessageDTO mediaDto, string userId, string baseUrl);
        Task<WebhookResponseDTO> ProcessWebhookAsync(IDictionary<string, string> formData);
        Task<ApiResponse<string>> ProcessStatusCallbackAsync(IDictionary<string, string> formData);
        Task<PaginatedResponse<WhatsAppMessage>> GetMessageHistoryAsync(string phoneNumber, int page, int pageSize);
        Task<PaginatedResponse<ConversationSummaryDTO>> GetActiveConversationsAsync(int page, int pageSize);
    }
}