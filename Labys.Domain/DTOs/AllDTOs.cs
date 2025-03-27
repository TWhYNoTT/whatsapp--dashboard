using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Labys.Domain.DTOs
{
    #region WhatsApp DTOs

    /// <summary>
    /// DTO for sending a simple WhatsApp message
    /// </summary>
    public class WhatsAppMessageDTO
    {
        [Required]
        [Phone]
        public string ToNumber { get; set; }

        [Required]
        public string Body { get; set; }

        public int? CustomerId { get; set; }

        public string? MediaUrl { get; set; }
    }

    /// <summary>
    /// DTO for sending template messages
    /// </summary>
    public class WhatsAppTemplateDTO
    {
        [Required]
        [Phone]
        public string ToNumber { get; set; }

        [Required]
        public string TemplateSid { get; set; }

        public List<string>? Variables { get; set; }

        public int? CustomerId { get; set; }
    }

    /// <summary>
    /// DTO for media messages
    /// </summary>
    public class WhatsAppMediaMessageDTO
    {
        [Required]
        public string ToNumber { get; set; }

        public string Caption { get; set; }

        public IFormFile Media { get; set; }

        public string MediaUrl { get; set; }

        public int? CustomerId { get; set; }
    }

    /// <summary>
    /// Response DTO for message operations
    /// </summary>
    public class MessageResponseDTO
    {
        public string MessageSid { get; set; }

        public string Status { get; set; }

        public string MediaUrl { get; set; }

        public string Error { get; set; }

        public bool Success { get; set; }
    }

    /// <summary>
    /// Webhook response DTO
    /// </summary>
    public class WebhookResponseDTO
    {
        public string Response { get; set; }

        public string ResponseType { get; set; } = "text/xml";
    }

    #endregion

    #region Contact DTOs

    /// <summary>
    /// DTO for contact operations
    /// </summary>
    public class ContactDTO
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Language { get; set; }

        public string Tags { get; set; }

        public bool HasOptedIn { get; set; }

        public string Notes { get; set; }

        public int? CustomerId { get; set; }
    }

    #endregion

    #region Template DTOs

    /// <summary>
    /// DTO for template operations
    /// </summary>
    public class WhatsAppTemplateCreationDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ContentSid { get; set; }

        public string Language { get; set; } = "en";

        public string Type { get; set; }
    }

    /// <summary>
    /// DTO for template approval
    /// </summary>
    public class TemplateApprovalDTO
    {
        public bool IsApproved { get; set; }
    }

    /// <summary>
    /// DTO for template testing
    /// </summary>
    public class TemplateTestDTO
    {
        [Required]
        public string TemplateSid { get; set; }

        [Required]
        [Phone]
        public string ToNumber { get; set; }

        public string Variable1 { get; set; }
        public string Variable2 { get; set; }
        public string Variable3 { get; set; }
    }

    #endregion

    #region Campaign DTOs

    /// <summary>
    /// DTO for campaign creation
    /// </summary>
    public class CampaignDTO
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string TemplateSid { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public string AudienceFilter { get; set; }

        public List<int> ContactIds { get; set; }

        public string Variable1 { get; set; }
        public string Variable2 { get; set; }
        public string Variable3 { get; set; }
    }

    /// <summary>
    /// DTO for campaign preview
    /// </summary>
    public class CampaignPreviewDTO
    {
        [Required]
        public string TemplateSid { get; set; }

        public string Variable1 { get; set; }
        public string Variable2 { get; set; }
        public string Variable3 { get; set; }
    }

    #endregion

    #region Conversation DTOs

    /// <summary>
    /// DTO for conversation summary
    /// </summary>
    public class ConversationSummaryDTO
    {
        public string ContactNumber { get; set; }

        public string ContactName { get; set; }

        public string LastMessage { get; set; }

        public DateTime LastMessageTime { get; set; }

        public int UnreadCount { get; set; }

        public bool IsAssigned { get; set; }

        public string AssignedAgentId { get; set; }

        public string AssignedAgentName { get; set; }

        public bool ChatbotDisabled { get; set; }
    }

    /// <summary>
    /// DTO for message history
    /// </summary>
    public class MessageHistoryDTO
    {
        public int Id { get; set; }

        public string Direction { get; set; }

        public string Body { get; set; }

        public DateTime Timestamp { get; set; }

        public string Status { get; set; }

        public bool IsAutomatedResponse { get; set; }

        public string MediaUrl { get; set; }
    }

    #endregion

    #region Chatbot DTOs

    /// <summary>
    /// DTO for chatbot rule
    /// </summary>
    public class ChatbotRuleDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Keywords { get; set; }

        [Required]
        public string Response { get; set; }

        public int Priority { get; set; } = 0;

        public bool ForwardToHuman { get; set; } = false;
    }

    /// <summary>
    /// DTO for chatbot response
    /// </summary>
    public class ChatbotResponseDTO
    {
        public string Message { get; set; }

        public bool ForwardToHuman { get; set; }

        public int? MatchedRuleId { get; set; }
    }

    /// <summary>
    /// DTO for testing chatbot
    /// </summary>
    public class ChatbotTestDTO
    {
        public string Message { get; set; }

        public int? CustomerId { get; set; }
    }

    #endregion

    #region Assignment DTOs

    /// <summary>
    /// DTO for conversation assignment
    /// </summary>
    public class ConversationAssignmentDTO
    {
        [Required]
        public string ContactNumber { get; set; }

        [Required]
        public string AgentId { get; set; }

        public bool DisableChatbot { get; set; } = true;

        public bool MarkAsRead { get; set; } = true;
    }

    /// <summary>
    /// DTO for releasing a conversation
    /// </summary>
    public class ConversationReleaseDTO
    {
        [Required]
        public string ContactNumber { get; set; }

        public bool KeepChatbotDisabled { get; set; } = false;
    }

    /// <summary>
    /// DTO for toggling chatbot for a conversation
    /// </summary>
    public class ChatbotToggleDTO
    {
        [Required]
        public string ContactNumber { get; set; }

        [Required]
        public bool DisableChatbot { get; set; }
    }

    /// <summary>
    /// DTO for updating agent activity
    /// </summary>
    public class ActivityUpdateDTO
    {
        [Required]
        public string ContactNumber { get; set; }

        public string ActivityStatus { get; set; }
    }

    /// <summary>
    /// DTO for assigned conversation
    /// </summary>
    public class AssignedConversationDTO
    {
        public string ContactNumber { get; set; }

        public string ContactName { get; set; }

        public string LastMessage { get; set; }

        public DateTime LastMessageTime { get; set; }

        public int UnreadCount { get; set; }

        public DateTime AssignedTime { get; set; }

        public string AgentId { get; set; }

        public string AgentName { get; set; }

        public bool ChatbotDisabled { get; set; }
    }

    #endregion

    #region Authentication DTOs

    /// <summary>
    /// DTO for user registration
    /// </summary>
    public class RegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public int Branch { get; set; }
    }

    /// <summary>
    /// DTO for user login
    /// </summary>
    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    #endregion

    #region Invoice DTOs

    /// <summary>
    /// DTO for invoice creation
    /// </summary>
    public class InvoiceDTO
    {
        public string SecondaryInvoiceID { get; set; }

        public decimal Price { get; set; }

        public string Stauts { get; set; }

        public string Address { get; set; }

        public string CustomerName { get; set; }

        public decimal MaintenanceCost { get; set; }

        public string PhoneNumber { get; set; }

        public IFormFile ProductImage { get; set; }

        public string AgreedDuration { get; set; }

        public decimal WeightOfPiece { get; set; }

        public int NumberOfPiece { get; set; }

        public string Notice { get; set; }

        public string BranchName { get; set; }

        public string ServiceType { get; set; }

        public DateTime DateOfMantanance { get; set; }

        public int InvoiceType { get; set; }

        public string MaintenanceCostType { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeNameR { get; set; }

        public int Branch { get; set; }

        public string ItemType { get; set; }
    }

    #endregion
}