using Labys.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Labys.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // WhatsApp entities
        public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; }
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; }
        public DbSet<WhatsAppCampaign> WhatsAppCampaigns { get; set; }
        public DbSet<CampaignRecipient> CampaignRecipients { get; set; }

        // Contact entities
        public DbSet<Contact> Contacts { get; set; }

        // Conversation entities
        public DbSet<ConversationAssignment> ConversationAssignments { get; set; }

        // Chatbot entities
        public DbSet<ChatbotRule> ChatbotRules { get; set; }

        // Notification entities
        public DbSet<Notification> Notifications { get; set; }

        // Invoice entities
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Count> Counts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and indexes

            // WhatsApp Messages
            modelBuilder.Entity<WhatsAppMessage>()
                .HasIndex(m => m.ContactNumber);

            modelBuilder.Entity<WhatsAppMessage>()
                .HasIndex(m => m.Timestamp);

            // Contacts
            modelBuilder.Entity<Contact>()
                .HasIndex(c => c.PhoneNumber)
                .IsUnique();

            // Campaign Recipients
            modelBuilder.Entity<CampaignRecipient>()
                .HasIndex(r => new { r.CampaignId, r.ContactId })
                .IsUnique();

            // WhatsApp Templates
            modelBuilder.Entity<WhatsAppTemplate>()
                .HasIndex(t => t.ContentSid)
                .IsUnique();

            // Conversation Assignments
            modelBuilder.Entity<ConversationAssignment>()
                .HasIndex(a => new { a.ContactNumber, a.IsActive });

            modelBuilder.Entity<ConversationAssignment>()
                .HasIndex(a => new { a.AgentId, a.IsActive });

            // Fix decimal precision warnings
            modelBuilder.Entity<Invoice>()
                .Property(i => i.MaintenanceCost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.WeightOfPiece)
                .HasColumnType("decimal(18,2)");
        }
    }
}