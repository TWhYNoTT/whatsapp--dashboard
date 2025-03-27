using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Labys.Application.Services.Implementations
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly IContactRepository _contactRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IWhatsAppMessageRepository _messageRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CampaignService> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public CampaignService(
            ICampaignRepository campaignRepository,
            IContactRepository contactRepository,
            ITemplateRepository templateRepository,
            IWhatsAppMessageRepository messageRepository,
            IConfiguration configuration,
            ILogger<CampaignService> logger,
            IServiceProvider serviceProvider)
        {
            _campaignRepository = campaignRepository;
            _contactRepository = contactRepository;
            _templateRepository = templateRepository;
            _messageRepository = messageRepository;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _accountSid = _configuration["Twilio:AccountSid"];
            _authToken = _configuration["Twilio:AuthToken"];
            _fromNumber = _configuration["Twilio:WhatsAppFromNumber"];
        }

        public async Task<ApiResponse<WhatsAppCampaign>> GetCampaignAsync(int id)
        {
            try
            {
                var campaign = await _campaignRepository.GetCampaignWithRecipientsAsync(id);
                if (campaign == null)
                {
                    return ApiResponse<WhatsAppCampaign>.ErrorResponse("Campaign not found");
                }

                return ApiResponse<WhatsAppCampaign>.SuccessResponse(campaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting campaign {Id}", id);
                return ApiResponse<WhatsAppCampaign>.ErrorResponse(ex.Message);
            }
        }

        public async Task<PaginatedResponse<WhatsAppCampaign>> GetCampaignsAsync(int page, int pageSize, string status = null)
        {
            try
            {
                IEnumerable<WhatsAppCampaign> campaigns;
                int totalCampaigns;

                if (!string.IsNullOrWhiteSpace(status))
                {
                    campaigns = await _campaignRepository.GetCampaignsByStatusAsync(status, page, pageSize);
                    totalCampaigns = await _campaignRepository.CountAsync(c => c.Status == status);
                }
                else
                {
                    campaigns = await _campaignRepository.FindAsync(c => true);
                    totalCampaigns = await _campaignRepository.CountAsync();

                    // Apply pagination manually
                    campaigns = campaigns
                        .OrderByDescending(c => c.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                return PaginatedResponse<WhatsAppCampaign>.CreatePaginated(
                    campaigns,
                    totalCampaigns,
                    pageSize,
                    page
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting campaigns");
                return new PaginatedResponse<WhatsAppCampaign>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<WhatsAppCampaign>> CreateCampaignAsync(WhatsAppCampaign campaign, IEnumerable<int> contactIds, string userId)
        {
            try
            {
                // Validate that template exists and is approved
                var template = await _templateRepository.GetByContentSidAsync(campaign.TemplateSid);
                if (template == null)
                {
                    return ApiResponse<WhatsAppCampaign>.ErrorResponse("Template not found");
                }

                if (!template.IsApproved)
                {
                    return ApiResponse<WhatsAppCampaign>.ErrorResponse("Cannot use unapproved template for campaign");
                }

                // Set default values
                campaign.CreatedAt = DateTime.UtcNow;
                campaign.CreatedBy = userId;
                campaign.Status = campaign.ScheduledDate.HasValue && campaign.ScheduledDate > DateTime.UtcNow
                    ? "scheduled"
                    : "draft";

                // Save the campaign
                campaign = await _campaignRepository.AddAsync(campaign);

                // Add recipients
                if (contactIds != null && contactIds.Any())
                {
                    foreach (var contactId in contactIds)
                    {
                        var contact = await _contactRepository.GetByIdAsync(contactId);
                        if (contact != null && contact.HasOptedIn)
                        {
                            var recipient = new CampaignRecipient
                            {
                                CampaignId = campaign.Id,
                                ContactId = contactId,
                                Status = "pending"
                            };

                            await _campaignRepository.AddRecipientAsync(recipient);
                        }
                    }

                    // Update total messages count
                    campaign.TotalMessages = contactIds.Count();
                    await _campaignRepository.UpdateAsync(campaign);
                }

                return ApiResponse<WhatsAppCampaign>.SuccessResponse(campaign, "Campaign created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign");
                return ApiResponse<WhatsAppCampaign>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<WhatsAppCampaign>> UpdateCampaignAsync(int id, WhatsAppCampaign campaign, IEnumerable<int> contactIds)
        {
            try
            {
                var existingCampaign = await _campaignRepository.GetByIdAsync(id);
                if (existingCampaign == null)
                {
                    return ApiResponse<WhatsAppCampaign>.ErrorResponse("Campaign not found");
                }

                // Can only update draft campaigns
                if (existingCampaign.Status != "draft")
                {
                    return ApiResponse<WhatsAppCampaign>.ErrorResponse("Only draft campaigns can be updated");
                }

                // Update campaign properties
                existingCampaign.Name = campaign.Name;
                existingCampaign.Description = campaign.Description;
                existingCampaign.TemplateSid = campaign.TemplateSid;
                existingCampaign.ScheduledDate = campaign.ScheduledDate;
                existingCampaign.Variable1 = campaign.Variable1;
                existingCampaign.Variable2 = campaign.Variable2;
                existingCampaign.Variable3 = campaign.Variable3;
                existingCampaign.AudienceFilter = campaign.AudienceFilter;

                // Update audience if contact IDs are provided
                if (contactIds != null && contactIds.Any())
                {
                    // For simplicity, just update the total messages count
                    existingCampaign.TotalMessages = contactIds.Count();
                }

                await _campaignRepository.UpdateAsync(existingCampaign);
                return ApiResponse<WhatsAppCampaign>.SuccessResponse(existingCampaign, "Campaign updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating campaign {Id}", id);
                return ApiResponse<WhatsAppCampaign>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> LaunchCampaignAsync(int id)
        {
            try
            {
                var campaign = await _campaignRepository.GetByIdAsync(id);
                if (campaign == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Campaign not found");
                }

                // Check if campaign can be launched
                if (campaign.Status != "draft" && campaign.Status != "scheduled")
                {
                    return ApiResponse<bool>.ErrorResponse("Only draft or scheduled campaigns can be launched");
                }

                // Update status
                campaign.Status = "in_progress";
                await _campaignRepository.UpdateAsync(campaign);

                // Process campaign directly here, no Task.Run to avoid thread issues
                await ProcessCampaignAsync(id);

                return ApiResponse<bool>.SuccessResponse(true, "Campaign launched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error launching campaign {Id}", id);
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> CancelCampaignAsync(int id)
        {
            try
            {
                var campaign = await _campaignRepository.GetByIdAsync(id);
                if (campaign == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Campaign not found");
                }

                // Check if campaign can be cancelled
                if (campaign.Status != "draft" && campaign.Status != "scheduled" && campaign.Status != "in_progress")
                {
                    return ApiResponse<bool>.ErrorResponse("This campaign cannot be cancelled");
                }

                // Update status
                campaign.Status = "cancelled";
                await _campaignRepository.UpdateAsync(campaign);

                return ApiResponse<bool>.SuccessResponse(true, "Campaign cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling campaign {Id}", id);
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteCampaignAsync(int id)
        {
            try
            {
                var campaign = await _campaignRepository.GetByIdAsync(id);
                if (campaign == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Campaign not found");
                }

                // Only allow deletion of draft campaigns
                if (campaign.Status != "draft")
                {
                    return ApiResponse<bool>.ErrorResponse("Only draft campaigns can be deleted");
                }

                await _campaignRepository.DeleteAsync(campaign);
                return ApiResponse<bool>.SuccessResponse(true, "Campaign deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting campaign {Id}", id);
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        public async Task<ApiResponse<Dictionary<string, object>>> GetCampaignAnalyticsAsync(int id)
        {
            try
            {
                var campaign = await _campaignRepository.GetCampaignWithRecipientsAsync(id);
                if (campaign == null)
                {
                    return ApiResponse<Dictionary<string, object>>.ErrorResponse("Campaign not found");
                }

                // Calculate analytics from recipients
                var statusCounts = campaign.Recipients?
                    .GroupBy(r => r.Status)
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<string, int>();

                var responseRate = campaign.Recipients?.Count > 0
                    ? (double)(campaign.Recipients?.Count(r => r.HasResponded) ?? 0) / campaign.Recipients.Count
                    : 0;

                var analytics = new Dictionary<string, object>
                {
                    ["CampaignId"] = campaign.Id,
                    ["CampaignName"] = campaign.Name,
                    ["Status"] = campaign.Status,
                    ["TotalRecipients"] = campaign.Recipients?.Count ?? 0,
                    ["StatusBreakdown"] = statusCounts,
                    ["ResponseRate"] = responseRate,
                    ["SentMessages"] = campaign.SentMessages,
                    ["DeliveredMessages"] = campaign.DeliveredMessages,
                    ["ReadMessages"] = campaign.ReadMessages,
                    ["FailedMessages"] = campaign.FailedMessages
                };

                return ApiResponse<Dictionary<string, object>>.SuccessResponse(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting campaign analytics for {Id}", id);
                return ApiResponse<Dictionary<string, object>>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Process scheduled campaigns that are due to be sent
        /// </summary>
        public async Task ProcessScheduledCampaignsAsync()
        {
            try
            {
                // Get scheduled campaigns that are due
                var now = DateTime.UtcNow;
                _logger.LogInformation("Checking for scheduled campaigns due before {Time}", now);

                var dueCampaigns = await _campaignRepository.FindAsync(
                    c => c.Status == "scheduled" && c.ScheduledDate.HasValue && c.ScheduledDate <= now);

                var campaignsList = dueCampaigns.ToList();
                _logger.LogInformation("Found {Count} campaigns to process", campaignsList.Count);

                foreach (var campaign in campaignsList)
                {
                    _logger.LogInformation("Processing campaign {Id}: {Name}", campaign.Id, campaign.Name);

                    // Update status
                    campaign.Status = "in_progress";
                    await _campaignRepository.UpdateAsync(campaign);

                    // Process campaign 
                    await ProcessCampaignAsync(campaign.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled campaigns");
                // Don't rethrow - this is a background job
            }
        }

        /// <summary>
        /// Process a single campaign with a fresh DbContext scope
        /// </summary>
        private async Task ProcessCampaignAsync(int campaignId)
        {
            _logger.LogInformation("Processing campaign {Id}", campaignId);

            // IMPORTANT: Create a new scope to avoid DbContext issues
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    // Get fresh repository instances from the new scope
                    var campaignRepo = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
                    var messageRepo = scope.ServiceProvider.GetRequiredService<IWhatsAppMessageRepository>();

                    // Get campaign details with fresh DbContext
                    var campaign = await campaignRepo.GetCampaignWithRecipientsAsync(campaignId);

                    if (campaign == null || campaign.Status != "in_progress")
                    {
                        _logger.LogWarning("Campaign {Id} not found or not in progress", campaignId);
                        return;
                    }

                    // Initialize Twilio client
                    TwilioClient.Init(_accountSid, _authToken);

                    // Format template variables
                    var variables = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(campaign.Variable1)) variables["1"] = campaign.Variable1;
                    if (!string.IsNullOrEmpty(campaign.Variable2)) variables["2"] = campaign.Variable2;
                    if (!string.IsNullOrEmpty(campaign.Variable3)) variables["3"] = campaign.Variable3;

                    string contentVariables = variables.Any()
                        ? JsonSerializer.Serialize(variables)
                        : null;

                    // Process recipients
                    if (campaign.Recipients != null)
                    {
                        var pendingRecipients = campaign.Recipients.Where(r => r.Status == "pending").ToList();
                        _logger.LogInformation("Processing {Count} recipients for campaign {Id}", pendingRecipients.Count, campaignId);

                        foreach (var recipient in pendingRecipients)
                        {
                            try
                            {
                                if (recipient.Contact == null || !recipient.Contact.HasOptedIn)
                                {
                                    _logger.LogWarning("Skipping recipient {Id}: contact null or not opted in", recipient.Id);
                                    recipient.Status = "skipped";
                                    await campaignRepo.UpdateRecipientStatusAsync(recipient.Id, "skipped");
                                    continue;
                                }

                                // Format phone numbers for WhatsApp
                                string formattedFromNumber = $"whatsapp:{_fromNumber}";
                                string formattedToNumber = $"whatsapp:{recipient.Contact.PhoneNumber}";

                                _logger.LogInformation("Sending message to {Number}", recipient.Contact.PhoneNumber);

                                // Send message
                                var messageResource = await MessageResource.CreateAsync(
                                    from: new PhoneNumber(formattedFromNumber),
                                    to: new PhoneNumber(formattedToNumber),
                                    body: null,
                                    contentSid: campaign.TemplateSid,
                                    contentVariables: contentVariables
                                );

                                _logger.LogInformation("Message sent with SID {Sid}", messageResource.Sid);

                                // Update recipient status
                                recipient.Status = "sent";
                                recipient.MessageSid = messageResource.Sid;
                                recipient.SentAt = DateTime.UtcNow;
                                recipient.StatusUpdatedAt = DateTime.UtcNow;
                                await campaignRepo.UpdateRecipientStatusAsync(recipient.Id, "sent");

                                // Store message record
                                var messageRecord = new WhatsAppMessage
                                {
                                    Sid = messageResource.Sid,
                                    ContactNumber = recipient.Contact.PhoneNumber,
                                    Body = $"Campaign: {campaign.Name}",
                                    Direction = "outbound",
                                    Status = "sent",
                                    Timestamp = DateTime.UtcNow,
                                    CustomerId = recipient.Contact.CustomerId,
                                    UserId = campaign.CreatedBy,
                                    TemplateId = campaign.TemplateSid,
                                    MediaUrl = "" // Empty string to prevent NULL
                                };

                                await messageRepo.AddAsync(messageRecord);

                                // Update campaign metrics
                                campaign.SentMessages++;
                                await campaignRepo.UpdateAsync(campaign);

                                // Add a small delay to avoid rate limiting
                                await Task.Delay(200);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing recipient {Id} for campaign {CampaignId}", recipient.Id, campaignId);

                                // Update recipient status to failed
                                recipient.Status = "failed";
                                recipient.StatusUpdatedAt = DateTime.UtcNow;
                                await campaignRepo.UpdateRecipientStatusAsync(recipient.Id, "failed");

                                // Update campaign metrics
                                campaign.FailedMessages++;
                                await campaignRepo.UpdateAsync(campaign);
                            }
                        }

                        // Check if all recipients are processed
                        var allProcessed = !campaign.Recipients.Any(r => r.Status == "pending");
                        if (allProcessed)
                        {
                            _logger.LogInformation("All recipients processed for campaign {Id}, marking as completed", campaignId);
                            campaign.Status = "completed";
                            await campaignRepo.UpdateAsync(campaign);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing campaign {Id}", campaignId);

                    // Update campaign status to error
                    try
                    {
                        var campaignRepo = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
                        var campaign = await campaignRepo.GetByIdAsync(campaignId);
                        if (campaign != null)
                        {
                            campaign.Status = "error";
                            await campaignRepo.UpdateAsync(campaign);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Error updating campaign status to error");
                    }
                }
            }
        }
    }
}