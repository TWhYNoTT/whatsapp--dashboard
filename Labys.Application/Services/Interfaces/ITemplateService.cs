using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;

namespace Labys.Application.Services.Interfaces
{
    /// <summary>
    /// Template service interface
    /// </summary>
    public interface ITemplateService
    {
        Task<ApiResponse<WhatsAppTemplate>> GetTemplateAsync(int id);
        Task<ApiResponse<WhatsAppTemplate>> GetTemplateByContentSidAsync(string contentSid);
        Task<PaginatedResponse<WhatsAppTemplate>> GetTemplatesAsync(int page, int pageSize, bool approvedOnly = false);
        Task<ApiResponse<WhatsAppTemplate>> CreateTemplateAsync(WhatsAppTemplate template, string userId);
        Task<ApiResponse<WhatsAppTemplate>> UpdateTemplateAsync(int id, WhatsAppTemplate template);
        Task<ApiResponse<bool>> DeleteTemplateAsync(int id);
        Task<ApiResponse<bool>> UpdateApprovalStatusAsync(int id, bool isApproved);
        Task<ApiResponse<MessageResponseDTO>> TestTemplateAsync(string templateSid, string toNumber, List<string> variables, string userId);
    }
}