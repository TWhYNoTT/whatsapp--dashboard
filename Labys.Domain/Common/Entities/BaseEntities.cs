using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Labys.Domain.Entities
{
    /// <summary>
    /// Application user entity extending IdentityUser
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public int Branch { get; set; }
    }

    /// <summary>
    /// Base message entity with common properties
    /// </summary>
    public abstract class BaseMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ContactNumber { get; set; }

        public string? Body { get; set; }

        [Required]
        public string Direction { get; set; }

        public string? Status { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; } = false;
    }

    /// <summary>
    /// WhatsApp message entity
    /// </summary>
    public class WhatsAppMessage : BaseMessage
    {
        // Twilio message SID
        public string? Sid { get; set; }

        // Related customer ID if available
        public int? CustomerId { get; set; }

        // Staff user who sent the message (for outbound)
        public string? UserId { get; set; }

        // For template messages
        public string? TemplateId { get; set; }

        // If this was an automated response
        public bool IsAutomatedResponse { get; set; } = false;

        // Any media URLs
        public string? MediaUrl { get; set; }
    }

    /// <summary>
    /// Contact entity for storing customer contact information
    /// </summary>
    public class Contact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Language { get; set; } = "en";

        public string? Tags { get; set; }

        public bool HasOptedIn { get; set; } = false;

        public DateTime? OptInDate { get; set; }

        public DateTime? LastContactDate { get; set; }

        public string? Notes { get; set; }

        public int? CustomerId { get; set; }
    }

    /// <summary>
    /// WhatsApp template entity
    /// </summary>
    public class WhatsAppTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ContentSid { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string Language { get; set; } = "en";

        public string? Type { get; set; }

        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// WhatsApp campaign entity
    /// </summary>
    public class WhatsAppCampaign
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? TemplateSid { get; set; }

        public string Status { get; set; } = "draft";

        public DateTime? ScheduledDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public string? AudienceFilter { get; set; }

        public int TotalMessages { get; set; }

        public int SentMessages { get; set; } = 0;

        public int DeliveredMessages { get; set; } = 0;

        public int ReadMessages { get; set; } = 0;

        public int FailedMessages { get; set; } = 0;

        public int Responses { get; set; } = 0;

        public string Variable1 { get; set; }

        public string Variable2 { get; set; }

        public string Variable3 { get; set; }

        public virtual ICollection<CampaignRecipient> Recipients { get; set; }
    }

    /// <summary>
    /// Campaign recipient entity
    /// </summary>
    public class CampaignRecipient
    {
        [Key]
        public int Id { get; set; }

        public int CampaignId { get; set; }

        public int ContactId { get; set; }

        public string Status { get; set; } = "pending";

        public string? MessageSid { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? StatusUpdatedAt { get; set; }

        public bool HasResponded { get; set; } = false;
        [JsonIgnore]
        [ForeignKey("CampaignId")]
        public virtual WhatsAppCampaign Campaign { get; set; }

        [ForeignKey("ContactId")]
        public virtual Contact Contact { get; set; }
    }

    /// <summary>
    /// Chatbot rule entity
    /// </summary>
    public class ChatbotRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Keywords { get; set; }

        [Required]
        public string Response { get; set; }

        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool ForwardToHuman { get; set; } = false;
    }

    /// <summary>
    /// Conversation assignment entity
    /// </summary>
    public class ConversationAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ContactNumber { get; set; }

        [Required]
        public string AgentId { get; set; }

        public DateTime AssignedTime { get; set; }

        public DateTime LastActivityTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool DisableChatbot { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("AgentId")]
        public virtual ApplicationUser Agent { get; set; }
    }

    /// <summary>
    /// Notification entity for staff notifications
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string Type { get; set; }

        public int? MessageId { get; set; }

        public string? ContactNumber { get; set; }

        public string? Content { get; set; }

        public bool IsHandled { get; set; } = false;

        public string? HandledBy { get; set; }

        public DateTime? HandledAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Invoice entity
    /// </summary>
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        public string SecondaryInvoiceID { get; set; }

        public decimal? Price { get; set; }

        public string Stauts { get; set; }

        public string CustomerName { get; set; }

        public string Address { get; set; }

        public DateTime DateOfMantanance { get; set; }

        public DateTime InitDate { get; set; }

        public decimal? MaintenanceCost { get; set; }

        public string PhoneNumber { get; set; }

        public string ProductImage { get; set; }

        public string AgreedDuration { get; set; }

        public decimal? WeightOfPiece { get; set; }

        public int? NumberOfPiece { get; set; }

        public string Notice { get; set; }

        public string BranchName { get; set; }

        public string ServiceType { get; set; }

        public int? InvoiceType { get; set; }

        public string MaintenanceCostType { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeNameR { get; set; }

        public int Branch { get; set; }

        public string ItemType { get; set; }
    }

    /// <summary>
    /// Count entity for branch statistics
    /// </summary>
    public class Count
    {
        [Key]
        public int Id { get; set; }

        public int Royal { get; set; } = 0;

        public int Grnata { get; set; } = 0;

        public int Aswak { get; set; } = 0;

        public int Factory { get; set; } = 0;
    }
}