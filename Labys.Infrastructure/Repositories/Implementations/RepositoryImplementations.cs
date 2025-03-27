using Labys.Domain.Entities;
using Labys.Infrastructure.Data;
using Labys.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Labys.Infrastructure.Repositories.Implementations
{
    /// <summary>
    /// Generic repository implementation
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }
    }

    /// <summary>
    /// WhatsApp message repository implementation
    /// </summary>
    public class WhatsAppMessageRepository : Repository<WhatsAppMessage>, IWhatsAppMessageRepository
    {
        public WhatsAppMessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<WhatsAppMessage>> GetMessagesByContactNumberAsync(string contactNumber, int page, int pageSize)
        {
            return await _dbSet
                .Where(m => m.ContactNumber == contactNumber)
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<WhatsAppMessage> GetLatestMessageByContactNumberAsync(string contactNumber)
        {
            return await _dbSet
                .Where(m => m.ContactNumber == contactNumber)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetUnreadCountByContactNumberAsync(string contactNumber)
        {
            return await _dbSet
                .CountAsync(m => m.ContactNumber == contactNumber && m.Direction == "inbound" && !m.IsRead);
        }

        public async Task MarkMessagesAsReadAsync(string contactNumber)
        {
            var unreadMessages = await _dbSet
                .Where(m => m.ContactNumber == contactNumber && m.Direction == "inbound" && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<string>> GetUniqueContactNumbersAsync(int page, int pageSize)
        {
            return await _dbSet
                .GroupBy(m => m.ContactNumber)
                .Select(g => g.Key)
                .OrderBy(n => n)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Contact repository implementation
    /// </summary>
    public class ContactRepository : Repository<Contact>, IContactRepository
    {
        public ContactRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Contact> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
        }

        public async Task<IEnumerable<Contact>> GetContactsByTagAsync(string tag, int page, int pageSize)
        {
            return await _dbSet
                .Where(c => c.Tags != null && c.Tags.Contains(tag))
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Contact>> SearchContactsAsync(string searchTerm, int page, int pageSize)
        {
            return await _dbSet
                .Where(c =>
                    c.Name.Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    c.Email.Contains(searchTerm))
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
        {
            return await _dbSet.AnyAsync(c => c.PhoneNumber == phoneNumber);
        }
    }

    /// <summary>
    /// Template repository implementation
    /// </summary>
    public class TemplateRepository : Repository<WhatsAppTemplate>, ITemplateRepository
    {
        public TemplateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WhatsAppTemplate> GetByContentSidAsync(string contentSid)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.ContentSid == contentSid);
        }

        public async Task<IEnumerable<WhatsAppTemplate>> GetApprovedTemplatesAsync()
        {
            return await _dbSet
                .Where(t => t.IsApproved)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<WhatsAppTemplate>> GetTemplatesByTypeAsync(string type)
        {
            return await _dbSet
                .Where(t => t.Type == type)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Campaign repository implementation
    /// </summary>
    public class CampaignRepository : Repository<WhatsAppCampaign>, ICampaignRepository
    {
        public CampaignRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<WhatsAppCampaign>> GetCampaignsByStatusAsync(string status, int page, int pageSize)
        {
            return await _dbSet
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<WhatsAppCampaign> GetCampaignWithRecipientsAsync(int campaignId)
        {
            return await _dbSet
                .Include(c => c.Recipients)
                .ThenInclude(r => r.Contact)
                .FirstOrDefaultAsync(c => c.Id == campaignId);
        }

        public async Task AddRecipientAsync(CampaignRecipient recipient)
        {
            await _context.Set<CampaignRecipient>().AddAsync(recipient);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRecipientStatusAsync(int recipientId, string status)
        {
            var recipient = await _context.Set<CampaignRecipient>().FindAsync(recipientId);
            if (recipient != null)
            {
                recipient.Status = status;
                recipient.StatusUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Chatbot rule repository implementation
    /// </summary>
    public class ChatbotRuleRepository : Repository<ChatbotRule>, IChatbotRuleRepository
    {
        public ChatbotRuleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ChatbotRule>> GetActiveRulesAsync()
        {
            return await _dbSet
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToListAsync();
        }

        public async Task<ChatbotRule> GetRuleByKeywordsAsync(string keywords)
        {
            return await _dbSet
                .Where(r => r.Keywords.Contains(keywords) && r.IsActive)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefaultAsync();
        }
    }

    /// <summary>
    /// Conversation assignment repository implementation
    /// </summary>
    public class ConversationAssignmentRepository : Repository<ConversationAssignment>, IConversationAssignmentRepository
    {
        public ConversationAssignmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ConversationAssignment> GetActiveAssignmentByContactNumberAsync(string contactNumber)
        {
            return await _dbSet
                .Include(a => a.Agent)
                .FirstOrDefaultAsync(a => a.ContactNumber == contactNumber && a.IsActive);
        }

        public async Task<IEnumerable<ConversationAssignment>> GetAssignmentsByAgentIdAsync(string agentId)
        {
            return await _dbSet
                .Include(a => a.Agent)
                .Where(a => a.AgentId == agentId && a.IsActive)
                .OrderByDescending(a => a.LastActivityTime)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Notification repository implementation
    /// </summary>
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetUnhandledNotificationsAsync()
        {
            return await _dbSet
                .Where(n => !n.IsHandled)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(string type)
        {
            return await _dbSet
                .Where(n => n.Type == type)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Invoice repository implementation
    /// </summary>
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByBranchAsync(string branchName, int page, int pageSize)
        {
            return await _dbSet
                .Where(i => i.BranchName == branchName)
                .OrderByDescending(i => i.InitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> SearchInvoicesAsync(string searchTerm, int page, int pageSize)
        {
            return await _dbSet
                .Where(i =>
                    i.CustomerName.Contains(searchTerm) ||
                    i.PhoneNumber.Contains(searchTerm) ||
                    i.SecondaryInvoiceID.Contains(searchTerm))
                .OrderByDescending(i => i.InitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}