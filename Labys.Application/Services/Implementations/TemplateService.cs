using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;


namespace Labys.Application.Services.Implementations
{
    /// <summary>
    /// Template service implementation
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ICampaignRepository _campaignRepository;
        private readonly IWhatsAppMessageRepository _messageRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IConfiguration _configuration;

        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public TemplateService(
            ITemplateRepository templateRepository,
            ICampaignRepository campaignRepository,
            IWhatsAppMessageRepository messageRepository,
            IContactRepository contactRepository,
            IConfiguration configuration)
        {
            _templateRepository = templateRepository;
            _campaignRepository = campaignRepository;
            _messageRepository = messageRepository;
            _contactRepository = contactRepository;
            _configuration = configuration;

            _accountSid = _configuration["Twilio:AccountSid"];
            _authToken = _configuration["Twilio:AuthToken"];
            _fromNumber = _configuration["Twilio:WhatsAppFromNumber"];
        }

        /// <summary>
        /// Get a template by ID
        /// </summary>
        public async Task<ApiResponse<WhatsAppTemplate>> GetTemplateAsync(int id)
        {
            try
            {
                var template = await _templateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return ApiResponse<WhatsAppTemplate>.ErrorResponse("Template not found");
                }

                return ApiResponse<WhatsAppTemplate>.SuccessResponse(template);
            }
            catch (Exception ex)
            {
                return ApiResponse<WhatsAppTemplate>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get a template by Twilio content SID
        /// </summary>
        public async Task<ApiResponse<WhatsAppTemplate>> GetTemplateByContentSidAsync(string contentSid)
        {
            try
            {
                var template = await _templateRepository.GetByContentSidAsync(contentSid);
                if (template == null)
                {
                    return ApiResponse<WhatsAppTemplate>.ErrorResponse("Template not found");
                }

                return ApiResponse<WhatsAppTemplate>.SuccessResponse(template);
            }
            catch (Exception ex)
            {
                return ApiResponse<WhatsAppTemplate>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get templates with pagination and filtering
        /// </summary>
        public async Task<PaginatedResponse<WhatsAppTemplate>> GetTemplatesAsync(int page, int pageSize, bool approvedOnly = false)
        {
            try
            {
                IEnumerable<WhatsAppTemplate> templates;
                int totalTemplates;

                if (approvedOnly)
                {
                    templates = await _templateRepository.GetApprovedTemplatesAsync();
                    totalTemplates = templates.Count();
                }
                else
                {
                    templates = await _templateRepository.GetAllAsync();
                    totalTemplates = await _templateRepository.CountAsync();
                }

                // Apply pagination manually
                templates = templates
                    .OrderBy(t => t.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PaginatedResponse<WhatsAppTemplate>.CreatePaginated(
                    templates,
                    totalTemplates,
                    pageSize,
                    page
                );
            }
            catch (Exception ex)
            {
                return new PaginatedResponse<WhatsAppTemplate>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Create a new template
        /// </summary>
        public async Task<ApiResponse<WhatsAppTemplate>> CreateTemplateAsync(WhatsAppTemplate template, string userId)
        {
            try
            {
                // Check if template with this SID already exists
                var existingTemplate = await _templateRepository.GetByContentSidAsync(template.ContentSid);
                if (existingTemplate != null)
                {
                    return ApiResponse<WhatsAppTemplate>.ErrorResponse("A template with this ContentSid already exists");
                }

                // Set default values
                template.CreatedAt = DateTime.UtcNow;
                template.CreatedBy = userId;
                template.IsApproved = false; // New templates start as unapproved

                var createdTemplate = await _templateRepository.AddAsync(template);
                return ApiResponse<WhatsAppTemplate>.SuccessResponse(createdTemplate, "Template created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<WhatsAppTemplate>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing template
        /// </summary>
        public async Task<ApiResponse<WhatsAppTemplate>> UpdateTemplateAsync(int id, WhatsAppTemplate template)
        {
            try
            {
                var existingTemplate = await _templateRepository.GetByIdAsync(id);
                if (existingTemplate == null)
                {
                    return ApiResponse<WhatsAppTemplate>.ErrorResponse("Template not found");
                }

                // Check if another template with this SID already exists
                var duplicateTemplate = await _templateRepository.GetByContentSidAsync(template.ContentSid);
                if (duplicateTemplate != null && duplicateTemplate.Id != id)
                {
                    return ApiResponse<WhatsAppTemplate>.ErrorResponse("Another template with this ContentSid already exists");
                }

                // Update properties
                existingTemplate.Name = template.Name;
                existingTemplate.Description = template.Description;
                existingTemplate.ContentSid = template.ContentSid;
                existingTemplate.Language = template.Language;
                existingTemplate.Type = template.Type;

                await _templateRepository.UpdateAsync(existingTemplate);
                return ApiResponse<WhatsAppTemplate>.SuccessResponse(existingTemplate, "Template updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<WhatsAppTemplate>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete a template
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteTemplateAsync(int id)
        {
            try
            {
                var template = await _templateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Template not found");
                }

                // Check if template is used in any campaigns
                var campaigns = await _campaignRepository.FindAsync(c => c.TemplateSid == template.ContentSid);
                if (campaigns.Any())
                {
                    return ApiResponse<bool>.ErrorResponse("Cannot delete template that is used in campaigns");
                }

                await _templateRepository.DeleteAsync(template);
                return ApiResponse<bool>.SuccessResponse(true, "Template deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update template approval status
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateApprovalStatusAsync(int id, bool isApproved)
        {
            try
            {
                var template = await _templateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Template not found");
                }

                template.IsApproved = isApproved;
                await _templateRepository.UpdateAsync(template);

                return ApiResponse<bool>.SuccessResponse(
                    isApproved,
                    $"Template {(isApproved ? "approved" : "unapproved")} successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Test a template by sending a message
        /// </summary>
        public async Task<ApiResponse<MessageResponseDTO>> TestTemplateAsync(string templateSid, string toNumber, List<string> variables, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateSid))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Template SID is required");
                }

                if (string.IsNullOrWhiteSpace(toNumber))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Recipient phone number is required");
                }

                // Format variables for template
                var contentVariables = string.Empty;
                if (variables != null && variables.Count > 0)
                {
                    var variablePairs = variables.Select((value, index) =>
                        $"\"{index + 1}\": \"{value}\"");
                    contentVariables = $"{{{string.Join(", ", variablePairs)}}}";
                }

                // Initialize Twilio client
                TwilioClient.Init(_accountSid, _authToken);

                // Send test message
                var messageResource = await MessageResource.CreateAsync(
                    from: new PhoneNumber($"whatsapp:{_fromNumber}"),
                    to: new PhoneNumber($"whatsapp:{toNumber}"),
                    body: null,
                    contentSid: templateSid,
                    contentVariables: contentVariables
                );

                // Record the test message
                var messageRecord = new WhatsAppMessage
                {
                    Sid = messageResource.Sid,
                    ContactNumber = toNumber,
                    Body = $"Template Test: {templateSid}",
                    Direction = "outbound",
                    Status = messageResource.Status.ToString(),
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    TemplateId = templateSid,
                    MediaUrl = "" // Empty string to prevent NULL
                };

                await _messageRepository.AddAsync(messageRecord);

                // Update contact's last contact date if exists
                var contact = await _contactRepository.GetByPhoneNumberAsync(toNumber);
                if (contact != null)
                {
                    contact.LastContactDate = DateTime.UtcNow;
                    await _contactRepository.UpdateAsync(contact);
                }

                return ApiResponse<MessageResponseDTO>.SuccessResponse(new MessageResponseDTO
                {
                    MessageSid = messageResource.Sid,
                    Status = messageResource.Status.ToString(),
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<MessageResponseDTO>.ErrorResponse(ex.Message);
            }
        }
    }
}