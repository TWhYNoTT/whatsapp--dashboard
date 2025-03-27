using Labys.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Labys.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository interface for common operations
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    }

    /// <summary>
    /// WhatsApp message repository interface
    /// </summary>
    public interface IWhatsAppMessageRepository : IRepository<WhatsAppMessage>
    {
        Task<IEnumerable<WhatsAppMessage>> GetMessagesByContactNumberAsync(string contactNumber, int page, int pageSize);
        Task<WhatsAppMessage> GetLatestMessageByContactNumberAsync(string contactNumber);
        Task<int> GetUnreadCountByContactNumberAsync(string contactNumber);
        Task MarkMessagesAsReadAsync(string contactNumber);
        Task<IEnumerable<string>> GetUniqueContactNumbersAsync(int page, int pageSize);
    }

    /// <summary>
    /// Contact repository interface
    /// </summary>
    public interface IContactRepository : IRepository<Contact>
    {
        Task<Contact> GetByPhoneNumberAsync(string phoneNumber);
        Task<IEnumerable<Contact>> GetContactsByTagAsync(string tag, int page, int pageSize);
        Task<IEnumerable<Contact>> SearchContactsAsync(string searchTerm, int page, int pageSize);
        Task<bool> PhoneNumberExistsAsync(string phoneNumber);
    }

    /// <summary>
    /// Template repository interface
    /// </summary>
    public interface ITemplateRepository : IRepository<WhatsAppTemplate>
    {
        Task<WhatsAppTemplate> GetByContentSidAsync(string contentSid);
        Task<IEnumerable<WhatsAppTemplate>> GetApprovedTemplatesAsync();
        Task<IEnumerable<WhatsAppTemplate>> GetTemplatesByTypeAsync(string type);
    }

    /// <summary>
    /// Campaign repository interface
    /// </summary>
    public interface ICampaignRepository : IRepository<WhatsAppCampaign>
    {
        Task<IEnumerable<WhatsAppCampaign>> GetCampaignsByStatusAsync(string status, int page, int pageSize);
        Task<WhatsAppCampaign> GetCampaignWithRecipientsAsync(int campaignId);
        Task AddRecipientAsync(CampaignRecipient recipient);
        Task UpdateRecipientStatusAsync(int recipientId, string status);
    }

    /// <summary>
    /// Chatbot rule repository interface
    /// </summary>
    public interface IChatbotRuleRepository : IRepository<ChatbotRule>
    {
        Task<IEnumerable<ChatbotRule>> GetActiveRulesAsync();
        Task<ChatbotRule> GetRuleByKeywordsAsync(string keywords);
    }

    /// <summary>
    /// Conversation assignment repository interface
    /// </summary>
    public interface IConversationAssignmentRepository : IRepository<ConversationAssignment>
    {
        Task<ConversationAssignment> GetActiveAssignmentByContactNumberAsync(string contactNumber);
        Task<IEnumerable<ConversationAssignment>> GetAssignmentsByAgentIdAsync(string agentId);
    }

    /// <summary>
    /// Notification repository interface
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUnhandledNotificationsAsync();
        Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type);
    }

    /// <summary>
    /// Invoice repository interface
    /// </summary>
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<IEnumerable<Invoice>> GetInvoicesByBranchAsync(string branchName, int page, int pageSize);
        Task<IEnumerable<Invoice>> SearchInvoicesAsync(string searchTerm, int page, int pageSize);
    }
}