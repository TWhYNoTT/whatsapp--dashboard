using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;

namespace Labys.Application.Services.Interfaces
{
    /// <summary>
    /// Conversation assignment service interface
    /// </summary>
    public interface IConversationAssignmentService
    {
        Task<ApiResponse<ConversationAssignment>> AssignConversationAsync(ConversationAssignmentDTO assignmentDto);
        Task<ApiResponse<bool>> ReleaseConversationAsync(ConversationReleaseDTO releaseDto, string currentUserId);
        Task<ApiResponse<bool>> ToggleChatbotAsync(ChatbotToggleDTO toggleDto, string currentUserId);
        Task<ApiResponse<IEnumerable<AssignedConversationDTO>>> GetUserAssignmentsAsync(string userId);
        Task<ApiResponse<bool>> UpdateActivityAsync(ActivityUpdateDTO activityDto, string currentUserId);
        Task<ApiResponse<ConversationAssignment>> GetAssignmentByContactNumberAsync(string contactNumber);
    }
}