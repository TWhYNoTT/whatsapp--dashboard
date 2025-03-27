using Labys.Domain.Common;
using Labys.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Labys.Application.Services.Interfaces
{
    public interface ICampaignService
    {
        Task<ApiResponse<WhatsAppCampaign>> GetCampaignAsync(int id);
        Task<PaginatedResponse<WhatsAppCampaign>> GetCampaignsAsync(int page, int pageSize, string status = null);
        Task<ApiResponse<WhatsAppCampaign>> CreateCampaignAsync(WhatsAppCampaign campaign, IEnumerable<int> contactIds, string userId);
        Task<ApiResponse<WhatsAppCampaign>> UpdateCampaignAsync(int id, WhatsAppCampaign campaign, IEnumerable<int> contactIds);
        Task<ApiResponse<bool>> LaunchCampaignAsync(int id);
        Task<ApiResponse<bool>> CancelCampaignAsync(int id);
        Task<ApiResponse<bool>> DeleteCampaignAsync(int id);
        Task<ApiResponse<Dictionary<string, object>>> GetCampaignAnalyticsAsync(int id);
        Task ProcessScheduledCampaignsAsync();
    }
}