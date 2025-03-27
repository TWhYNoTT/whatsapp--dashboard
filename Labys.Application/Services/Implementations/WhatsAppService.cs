using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.Types;


namespace Labys.Application.Services.Implementations
{
    /// <summary>
    /// WhatsApp service implementation
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IWhatsAppMessageRepository _messageRepository;
        private readonly IContactRepository _contactRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IChatbotRuleRepository _chatbotRuleRepository;
        private readonly IConversationAssignmentRepository _assignmentRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IConfiguration _configuration;

        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public WhatsAppService(
            IWhatsAppMessageRepository messageRepository,
            IContactRepository contactRepository,
            ITemplateRepository templateRepository,
            IChatbotRuleRepository chatbotRuleRepository,
            IConversationAssignmentRepository assignmentRepository,
            INotificationRepository notificationRepository,
            IConfiguration configuration)
        {
            _messageRepository = messageRepository;
            _contactRepository = contactRepository;
            _templateRepository = templateRepository;
            _chatbotRuleRepository = chatbotRuleRepository;
            _assignmentRepository = assignmentRepository;
            _notificationRepository = notificationRepository;
            _configuration = configuration;

            _accountSid = _configuration["Twilio:AccountSid"];
            _authToken = _configuration["Twilio:AuthToken"];
            _fromNumber = _configuration["Twilio:WhatsAppFromNumber"];
        }

        /// <summary>
        /// Send a plain text WhatsApp message
        /// </summary>
        public async Task<ApiResponse<MessageResponseDTO>> SendMessageAsync(WhatsAppMessageDTO messageDto, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(messageDto.ToNumber))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Recipient phone number is required");
                }

                if (string.IsNullOrWhiteSpace(messageDto.Body) && string.IsNullOrWhiteSpace(messageDto.MediaUrl))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Either message body or media URL is required");
                }

                // Initialize Twilio client
                TwilioClient.Init(_accountSid, _authToken);

                // Format phone numbers for WhatsApp
                string formattedFromNumber = $"whatsapp:{_fromNumber}";
                string formattedToNumber = $"whatsapp:{messageDto.ToNumber}";

                // Create the message resource
                var messageResource = await MessageResource.CreateAsync(
                    from: new PhoneNumber(formattedFromNumber),
                    to: new PhoneNumber(formattedToNumber),
                    body: messageDto.Body,
                    mediaUrl: !string.IsNullOrEmpty(messageDto.MediaUrl) ? new List<Uri> { new Uri(messageDto.MediaUrl) } : null
                );

                // Store message in database
                var messageRecord = new WhatsAppMessage
                {
                    Sid = messageResource.Sid,
                    ContactNumber = messageDto.ToNumber,
                    Body = messageDto.Body,
                    Direction = "outbound",
                    Status = messageResource.Status.ToString(),
                    Timestamp = DateTime.UtcNow,
                    CustomerId = messageDto.CustomerId,
                    UserId = userId,
                    MediaUrl = messageDto.MediaUrl ?? ""
                };

                await _messageRepository.AddAsync(messageRecord);

                // Update contact's last contact date if exists
                var contact = await _contactRepository.GetByPhoneNumberAsync(messageDto.ToNumber);
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

        /// <summary>
        /// Send a template WhatsApp message
        /// </summary>
        public async Task<ApiResponse<MessageResponseDTO>> SendTemplateMessageAsync(WhatsAppTemplateDTO templateDto, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateDto.ToNumber))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Recipient phone number is required");
                }

                if (string.IsNullOrWhiteSpace(templateDto.TemplateSid))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Template SID is required");
                }

                // Format variables for template
                var contentVariables = string.Empty;
                if (templateDto.Variables != null && templateDto.Variables.Count > 0)
                {
                    var variables = templateDto.Variables.Select((value, index) =>
                        $"\"{index + 1}\": \"{value}\"");
                    contentVariables = $"{{{string.Join(", ", variables)}}}";
                }

                // Initialize Twilio client
                TwilioClient.Init(_accountSid, _authToken);

                // Create the message resource
                var messageResource = await MessageResource.CreateAsync(
                    from: new PhoneNumber($"whatsapp:{_fromNumber}"),
                    to: new PhoneNumber($"whatsapp:{templateDto.ToNumber}"),
                    body: null,
                    contentSid: templateDto.TemplateSid,
                    contentVariables: contentVariables
                );

                // Store message in database
                var messageRecord = new WhatsAppMessage
                {
                    Sid = messageResource.Sid,
                    ContactNumber = templateDto.ToNumber,
                    Body = $"Template: {templateDto.TemplateSid}",
                    Direction = "outbound",
                    Status = messageResource.Status.ToString(),
                    Timestamp = DateTime.UtcNow,
                    CustomerId = templateDto.CustomerId,
                    UserId = userId,
                    TemplateId = templateDto.TemplateSid,
                    MediaUrl = "" // Empty string to prevent NULL
                };

                await _messageRepository.AddAsync(messageRecord);

                // Update contact's last contact date if exists
                var contact = await _contactRepository.GetByPhoneNumberAsync(templateDto.ToNumber);
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

        /// <summary>
        /// Send a media WhatsApp message
        /// </summary>
        public async Task<ApiResponse<MessageResponseDTO>> SendMediaMessageAsync(WhatsAppMediaMessageDTO mediaDto, string userId, string baseUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaDto.ToNumber))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Recipient phone number is required");
                }

                if (mediaDto.Media == null && string.IsNullOrWhiteSpace(mediaDto.MediaUrl))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("Either media file or media URL is required");
                }

                // Initialize Twilio client
                TwilioClient.Init(_accountSid, _authToken);

                // Handle file upload
                string mediaUrl = null;
                if (mediaDto.Media != null)
                {
                    // Save file to wwwroot/media folder
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaDto.Media.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mediaDto.Media.CopyToAsync(stream);
                    }

                    // Generate public URL
                    mediaUrl = $"{baseUrl}/media/{uniqueFileName}";
                }
                else if (!string.IsNullOrEmpty(mediaDto.MediaUrl))
                {
                    mediaUrl = mediaDto.MediaUrl;
                }

                if (string.IsNullOrEmpty(mediaUrl))
                {
                    return ApiResponse<MessageResponseDTO>.ErrorResponse("No media provided or media upload failed.");
                }

                // Format phone numbers
                string formattedFromNumber = $"whatsapp:{_fromNumber}";
                string formattedToNumber = $"whatsapp:{mediaDto.ToNumber}";

                // Send message
                var messageResource = await MessageResource.CreateAsync(
                    from: new PhoneNumber(formattedFromNumber),
                    to: new PhoneNumber(formattedToNumber),
                    body: mediaDto.Caption,
                    mediaUrl: new List<Uri> { new Uri(mediaUrl) }
                );

                // Store in database
                var messageRecord = new WhatsAppMessage
                {
                    Sid = messageResource.Sid,
                    ContactNumber = mediaDto.ToNumber,
                    Body = mediaDto.Caption ?? "Media message",
                    Direction = "outbound",
                    Status = messageResource.Status.ToString(),
                    Timestamp = DateTime.UtcNow,
                    CustomerId = mediaDto.CustomerId,
                    UserId = userId,
                    MediaUrl = mediaUrl
                };

                await _messageRepository.AddAsync(messageRecord);

                // Update contact's last contact date if exists
                var contact = await _contactRepository.GetByPhoneNumberAsync(mediaDto.ToNumber);
                if (contact != null)
                {
                    contact.LastContactDate = DateTime.UtcNow;
                    await _contactRepository.UpdateAsync(contact);
                }

                return ApiResponse<MessageResponseDTO>.SuccessResponse(new MessageResponseDTO
                {
                    MessageSid = messageResource.Sid,
                    Status = messageResource.Status.ToString(),
                    MediaUrl = mediaUrl,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<MessageResponseDTO>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Process incoming webhook from Twilio
        /// </summary>
        public async Task<WebhookResponseDTO> ProcessWebhookAsync(IDictionary<string, string> formData)
        {
            try
            {
                // Extract message data from the request
                string messageSid = formData.ContainsKey("MessageSid") ? formData["MessageSid"] : null;
                string from = formData.ContainsKey("From") ? formData["From"] : null;
                string to = formData.ContainsKey("To") ? formData["To"] : null;
                string body = formData.ContainsKey("Body") ? formData["Body"] : null;
                string profileName = formData.ContainsKey("ProfileName") ? formData["ProfileName"] : null;

                // Process media if present
                int numMedia = 0;
                string mediaUrl = null;
                if (formData.ContainsKey("NumMedia") && int.TryParse(formData["NumMedia"], out numMedia) && numMedia > 0)
                {
                    mediaUrl = formData["MediaUrl0"];
                }

                // Clean phone numbers
                string cleanedFromNumber = from.Replace("whatsapp:", "");

                // Create or update contact
                var contact = await EnsureContactExistsAsync(cleanedFromNumber, profileName);

                // Store incoming message in database
                var messageRecord = new WhatsAppMessage
                {
                    Sid = messageSid,
                    ContactNumber = cleanedFromNumber,
                    Body = body,
                    Direction = "inbound",
                    Status = "received",
                    Timestamp = DateTime.UtcNow,
                    CustomerId = contact.CustomerId,
                    MediaUrl = mediaUrl ?? "" // Use empty string instead of null
                };

                await _messageRepository.AddAsync(messageRecord);

                // Check if this conversation has been assigned and chatbot is disabled
                var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(cleanedFromNumber);
                bool disableChatbot = assignment != null && assignment.DisableChatbot;

                // Set up response
                var response = new MessagingResponse();

                if (disableChatbot)
                {
                    // Chatbot is disabled - notify agent and don't send automated response

                    // Update assignment last activity time
                    if (assignment != null)
                    {
                        assignment.LastActivityTime = DateTime.UtcNow;
                        await _assignmentRepository.UpdateAsync(assignment);
                    }

                    // Create a notification for the assigned agent
                    var notification = new Notification
                    {
                        Type = "NewMessage",
                        ContactNumber = cleanedFromNumber,
                        Content = $"New message from {contact.Name}: {body}",
                        IsHandled = false,
                        CreatedAt = DateTime.UtcNow,
                        HandledBy = null,
                        MessageId = messageRecord.Id
                    };

                    await _notificationRepository.AddAsync(notification);

                    // No automated response - agent will respond manually
                }
                else
                {
                    // Process with chatbot
                    var chatbotResponse = await ProcessChatbotResponseAsync(body, cleanedFromNumber, contact.CustomerId);

                    if (!string.IsNullOrEmpty(chatbotResponse.Message) && !chatbotResponse.ForwardToHuman)
                    {
                        // Send automatic response via TwiML
                        response.Message(chatbotResponse.Message);

                        // Record automated response
                        var autoResponseRecord = new WhatsAppMessage
                        {
                            ContactNumber = cleanedFromNumber,
                            Body = chatbotResponse.Message,
                            Direction = "outbound",
                            Status = "sent",
                            Timestamp = DateTime.UtcNow,
                            CustomerId = contact.CustomerId,
                            IsAutomatedResponse = true,
                            MediaUrl = "" // Empty string to prevent NULL
                        };

                        await _messageRepository.AddAsync(autoResponseRecord);
                    }
                    else if (chatbotResponse.ForwardToHuman)
                    {
                        // Human attention needed but no immediate response
                        // Create a notification
                        var notification = new Notification
                        {
                            Type = "HumanNeeded",
                            ContactNumber = cleanedFromNumber,
                            Content = $"Message requires human attention: {body}",
                            IsHandled = false,
                            CreatedAt = DateTime.UtcNow,
                            HandledBy = null,
                            MessageId = messageRecord.Id
                        };

                        await _notificationRepository.AddAsync(notification);
                    }
                }

                // Update contact's last contact date
                contact.LastContactDate = DateTime.UtcNow;
                await _contactRepository.UpdateAsync(contact);

                return new WebhookResponseDTO
                {
                    Response = response.ToString(),
                    ResponseType = "application/xml"
                };
            }
            catch (Exception ex)
            {
                // Return a generic response to avoid errors in Twilio
                var fallbackResponse = new MessagingResponse();
                fallbackResponse.Message("Thank you for your message. We'll get back to you shortly.");

                return new WebhookResponseDTO
                {
                    Response = fallbackResponse.ToString(),
                    ResponseType = "application/xml"
                };
            }
        }

        /// <summary>
        /// Process status callback from Twilio
        /// </summary>
        public async Task<ApiResponse<string>> ProcessStatusCallbackAsync(IDictionary<string, string> formData)
        {
            try
            {
                string messageSid = formData.ContainsKey("MessageSid") ? formData["MessageSid"] : null;
                string messageStatus = formData.ContainsKey("MessageStatus") ? formData["MessageStatus"] : null;
                string to = formData.ContainsKey("To") ? formData["To"] : null;

                if (string.IsNullOrEmpty(messageSid) || string.IsNullOrEmpty(messageStatus))
                {
                    return ApiResponse<string>.ErrorResponse("Required parameters missing from status callback");
                }

                // Find the message by SID
                var messages = await _messageRepository.FindAsync(m => m.Sid == messageSid);
                var message = messages.FirstOrDefault();

                if (message != null)
                {
                    // Update message status
                    message.Status = messageStatus;
                    await _messageRepository.UpdateAsync(message);
                }

                return ApiResponse<string>.SuccessResponse(messageStatus, "Status callback processed successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get message history for a contact
        /// </summary>
        public async Task<PaginatedResponse<WhatsAppMessage>> GetMessageHistoryAsync(string phoneNumber, int page, int pageSize)
        {
            try
            {
                // Clean phone number
                phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

                // Get messages for the contact
                var messages = await _messageRepository.GetMessagesByContactNumberAsync(phoneNumber, page, pageSize);
                var totalMessages = await _messageRepository.CountAsync(m => m.ContactNumber == phoneNumber);

                // Mark unread messages as read
                await _messageRepository.MarkMessagesAsReadAsync(phoneNumber);

                return PaginatedResponse<WhatsAppMessage>.CreatePaginated(
                    messages,
                    totalMessages,
                    pageSize,
                    page
                );
            }
            catch (Exception ex)
            {
                return new PaginatedResponse<WhatsAppMessage>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get active conversations
        /// </summary>
        public async Task<PaginatedResponse<ConversationSummaryDTO>> GetActiveConversationsAsync(int page, int pageSize)
        {
            try
            {
                // Get unique contact numbers with pagination
                var contactNumbers = await _messageRepository.GetUniqueContactNumbersAsync(page, pageSize);
                var totalContacts = await _messageRepository.CountAsync(m => true);

                // Process each contact to build conversation summaries
                var conversations = new List<ConversationSummaryDTO>();

                foreach (var phoneNumber in contactNumbers)
                {
                    // Get contact information
                    var contact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);

                    // Get latest message
                    var latestMessage = await _messageRepository.GetLatestMessageByContactNumberAsync(phoneNumber);

                    // Get unread count
                    var unreadCount = await _messageRepository.GetUnreadCountByContactNumberAsync(phoneNumber);

                    // Get assignment status
                    var assignment = await _assignmentRepository.GetActiveAssignmentByContactNumberAsync(phoneNumber);

                    conversations.Add(new ConversationSummaryDTO
                    {
                        ContactNumber = phoneNumber,
                        ContactName = contact?.Name ?? "Unknown",
                        LastMessage = latestMessage?.Body,
                        LastMessageTime = latestMessage?.Timestamp ?? DateTime.MinValue,
                        UnreadCount = unreadCount,
                        IsAssigned = assignment != null,
                        AssignedAgentId = assignment?.AgentId,
                        AssignedAgentName = assignment?.Agent?.UserName,
                        ChatbotDisabled = assignment?.DisableChatbot ?? false
                    });
                }

                return PaginatedResponse<ConversationSummaryDTO>.CreatePaginated(
                    conversations,
                    totalContacts,
                    pageSize,
                    page
                );
            }
            catch (Exception ex)
            {
                return new PaginatedResponse<ConversationSummaryDTO>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        #region Helper Methods

        /// <summary>
        /// Ensure a contact exists in the database
        /// </summary>
        private async Task<Contact> EnsureContactExistsAsync(string phoneNumber, string name = null)
        {
            // Clean the phone number
            phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

            // Check if contact already exists
            var existingContact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);
            if (existingContact != null)
            {
                return existingContact;
            }

            // Create new contact
            var newContact = new Contact
            {
                PhoneNumber = phoneNumber,
                Name = name ?? "New WhatsApp Customer",
                HasOptedIn = true,  // They messaged us first, so implied opt-in
                OptInDate = DateTime.UtcNow,
                LastContactDate = DateTime.UtcNow,
                Tags = "whatsapp,auto-created",
                Notes = "Automatically created from WhatsApp message"
            };

            await _contactRepository.AddAsync(newContact);

            return newContact;
        }

        /// <summary>
        /// Process a message with the chatbot rules
        /// </summary>
        private async Task<ChatbotResponseDTO> ProcessChatbotResponseAsync(string message, string phoneNumber, int? customerId = null)
        {
            try
            {
                // Get all active rules ordered by priority
                var rules = await _chatbotRuleRepository.GetActiveRulesAsync();
                var orderedRules = rules.OrderByDescending(r => r.Priority).ToList();

                // Default response if no rules match
                var response = new ChatbotResponseDTO
                {
                    Message = "Thank you for your message. Our team will get back to you shortly.",
                    ForwardToHuman = true
                };

                // Try to match the message against rules
                foreach (var rule in orderedRules)
                {
                    // Split keywords and check if any match
                    var keywords = rule.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim().ToLower())
                        .Where(k => !string.IsNullOrWhiteSpace(k))
                        .ToArray();

                    bool isMatch = false;

                    // Check if the message contains any of the keywords
                    foreach (var keyword in keywords)
                    {
                        // Use word boundary to ensure we're matching whole words
                        if (System.Text.RegularExpressions.Regex.IsMatch(message.ToLower(), $"\\b{System.Text.RegularExpressions.Regex.Escape(keyword)}\\b"))
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        string responseText = rule.Response;

                        // Personalize the response if customer info is available
                        if (customerId.HasValue)
                        {
                            // In a real implementation, get customer details and personalize
                            // responseText = responseText.Replace("{CustomerName}", customerName);
                        }

                        response = new ChatbotResponseDTO
                        {
                            Message = responseText,
                            ForwardToHuman = rule.ForwardToHuman,
                            MatchedRuleId = rule.Id
                        };

                        break;
                    }
                }

                return response;
            }
            catch (Exception)
            {
                return new ChatbotResponseDTO
                {
                    Message = "Thank you for your message. Our team will get back to you shortly.",
                    ForwardToHuman = true
                };
            }
        }

        #endregion
    }
}