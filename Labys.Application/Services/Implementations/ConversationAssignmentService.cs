using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace Labys.Application.Services.Implementations
{
    /// <summary>
    /// Conversation assignment service implementation
    /// </summary>
    public class ConversationAssignmentService : IConversationAssignmentService
    {
        private readonly IConversationAssignmentRepository _assignmentRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IWhatsAppMessageRepository _messageRepository;

        public ConversationAssignmentService(
            IConversationAssignmentRepository assignmentRepository,
            IContactRepository contactRepository,
            IWhatsAppMessageRepository messageRepository)
        {
            _assignmentRepository = assignmentRepository;
            _contactRepository = contactRepository;
            _messageRepository = messageRepository;
        }

        /// <summary>
        /// Assign a conversation to an agent
        /// </summary>
        public async Task<ApiResponse<ConversationAssignment>> AssignConversationAsync(ConversationAssignmentDTO assignmentDto)
        {
            try
            {
                // Validate contact exists
                var contact = await _contactRepository.GetByPhoneNumberAsync(assignmentDto.ContactNumber);
                if (contact == null)
                {
                    return ApiResponse<ConversationAssignment>.ErrorResponse("Contact number does not exist.");
                }

                // Check if conversation is already assigned
                var existingAssignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(assignmentDto.ContactNumber);

                if (existingAssignment != null)
                {
                    // Update existing assignment
                    existingAssignment.AgentId = assignmentDto.AgentId;
                    existingAssignment.DisableChatbot = assignmentDto.DisableChatbot;
                    existingAssignment.LastActivityTime = DateTime.UtcNow;

                    await _assignmentRepository.UpdateAsync(existingAssignment);
                }
                else
                {
                    // Create new assignment
                    existingAssignment = new ConversationAssignment
                    {
                        ContactNumber = assignmentDto.ContactNumber,
                        AgentId = assignmentDto.AgentId,
                        AssignedTime = DateTime.UtcNow,
                        LastActivityTime = DateTime.UtcNow,
                        DisableChatbot = assignmentDto.DisableChatbot,
                        IsActive = true
                    };

                    await _assignmentRepository.AddAsync(existingAssignment);
                }

                // Mark messages as read if requested
                if (assignmentDto.MarkAsRead)
                {
                    await _messageRepository.MarkMessagesAsReadAsync(assignmentDto.ContactNumber);
                }

                return ApiResponse<ConversationAssignment>.SuccessResponse(existingAssignment, "Conversation assigned successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConversationAssignment>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Release a conversation from an agent
        /// </summary>
        public async Task<ApiResponse<bool>> ReleaseConversationAsync(ConversationReleaseDTO releaseDto, string currentUserId)
        {
            try
            {
                // Find active assignment
                var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(releaseDto.ContactNumber);
                if (assignment == null)
                {
                    return ApiResponse<bool>.ErrorResponse("No active assignment found for this contact.");
                }

                // Verify agent is the assigned agent or a supervisor (would require role checking in a real implementation)
                // For now, we just check if the current user is the assigned agent
                if (assignment.AgentId != currentUserId)
                {
                    return ApiResponse<bool>.ErrorResponse("You do not have permission to release this assignment.");
                }

                // Release the assignment
                assignment.IsActive = false;
                assignment.EndTime = DateTime.UtcNow;

                // Enable chatbot if requested
                if (!releaseDto.KeepChatbotDisabled)
                {
                    assignment.DisableChatbot = false;
                }

                await _assignmentRepository.UpdateAsync(assignment);

                return ApiResponse<bool>.SuccessResponse(true, "Conversation released successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Toggle chatbot for a conversation
        /// </summary>
        public async Task<ApiResponse<bool>> ToggleChatbotAsync(ChatbotToggleDTO toggleDto, string currentUserId)
        {
            try
            {
                // Find active assignment
                var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(toggleDto.ContactNumber);

                if (assignment == null)
                {
                    // Create a new assignment if it doesn't exist
                    assignment = new ConversationAssignment
                    {
                        ContactNumber = toggleDto.ContactNumber,
                        AgentId = currentUserId,
                        AssignedTime = DateTime.UtcNow,
                        LastActivityTime = DateTime.UtcNow,
                        DisableChatbot = toggleDto.DisableChatbot,
                        IsActive = true
                    };

                    await _assignmentRepository.AddAsync(assignment);
                }
                else
                {
                    // Verify agent is the assigned agent or a supervisor
                    if (assignment.AgentId != currentUserId)
                    {
                        return ApiResponse<bool>.ErrorResponse("You do not have permission to modify this assignment.");
                    }

                    // Update existing assignment
                    assignment.DisableChatbot = toggleDto.DisableChatbot;
                    assignment.LastActivityTime = DateTime.UtcNow;

                    await _assignmentRepository.UpdateAsync(assignment);
                }

                return ApiResponse<bool>.SuccessResponse(true, $"Chatbot {(toggleDto.DisableChatbot ? "disabled" : "enabled")} successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all conversations assigned to a user
        /// </summary>
        public async Task<ApiResponse<IEnumerable<AssignedConversationDTO>>> GetUserAssignmentsAsync(string userId)
        {
            try
            {
                // Get all assignments for the user
                var assignments = await _assignmentRepository.GetAssignmentsByAgentIdAsync(userId);

                var result = new List<AssignedConversationDTO>();

                foreach (var assignment in assignments)
                {
                    // Get contact info
                    var contact = await _contactRepository.GetByPhoneNumberAsync(assignment.ContactNumber);

                    // Get latest message
                    var latestMessage = await _messageRepository.GetLatestMessageByContactNumberAsync(assignment.ContactNumber);

                    // Get unread count
                    var unreadCount = await _messageRepository.GetUnreadCountByContactNumberAsync(assignment.ContactNumber);

                    result.Add(new AssignedConversationDTO
                    {
                        ContactNumber = assignment.ContactNumber,
                        ContactName = contact?.Name ?? "Unknown",
                        LastMessage = latestMessage?.Body,
                        LastMessageTime = latestMessage?.Timestamp ?? DateTime.MinValue,
                        UnreadCount = unreadCount,
                        AssignedTime = assignment.AssignedTime,
                        AgentId = assignment.AgentId,
                        AgentName = assignment.Agent?.UserName ?? "Unknown",
                        ChatbotDisabled = assignment.DisableChatbot
                    });
                }

                return ApiResponse<IEnumerable<AssignedConversationDTO>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<AssignedConversationDTO>>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update agent activity for a conversation
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateActivityAsync(ActivityUpdateDTO activityDto, string currentUserId)
        {
            try
            {
                // Find active assignment
                var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(activityDto.ContactNumber);
                if (assignment == null)
                {
                    return ApiResponse<bool>.ErrorResponse("No active assignment found for this contact.");
                }

                // Verify agent is the assigned agent
                if (assignment.AgentId != currentUserId)
                {
                    return ApiResponse<bool>.ErrorResponse("You do not have permission to update this assignment.");
                }

                // Update last activity time
                assignment.LastActivityTime = DateTime.UtcNow;

                await _assignmentRepository.UpdateAsync(assignment);

                return ApiResponse<bool>.SuccessResponse(true, "Activity updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get an assignment by contact number
        /// </summary>
        public async Task<ApiResponse<ConversationAssignment>> GetAssignmentByContactNumberAsync(string contactNumber)
        {
            try
            {
                var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(contactNumber);

                if (assignment == null)
                {
                    return ApiResponse<ConversationAssignment>.ErrorResponse("No active assignment found for this contact.");
                }

                return ApiResponse<ConversationAssignment>.SuccessResponse(assignment);
            }
            catch (Exception ex)
            {
                return ApiResponse<ConversationAssignment>.ErrorResponse(ex.Message);
            }
        }
    }
}