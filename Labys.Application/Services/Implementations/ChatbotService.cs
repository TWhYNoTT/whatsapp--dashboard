using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace Labys.Application.Services.Implementations
{
    /// <summary>
    /// Chatbot service implementation
    /// </summary>
    public class ChatbotService : IChatbotService
    {
        private readonly IChatbotRuleRepository _ruleRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public ChatbotService(
            IChatbotRuleRepository ruleRepository,
            INotificationRepository notificationRepository,
            IContactRepository contactRepository,
            IInvoiceRepository invoiceRepository)
        {
            _ruleRepository = ruleRepository;
            _notificationRepository = notificationRepository;
            _contactRepository = contactRepository;
            _invoiceRepository = invoiceRepository;
        }

        /// <summary>
        /// Process an incoming message and generate a response
        /// </summary>
        public async Task<ApiResponse<ChatbotResponseDTO>> ProcessIncomingMessageAsync(string message, string phoneNumber, int? customerId = null)
        {
            try
            {
                // If no customerId is provided, try to find it
                if (!customerId.HasValue)
                {
                    var contact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);
                    customerId = contact?.CustomerId;
                }

                // Get customer information if available
                Invoice customer = null;
                if (customerId.HasValue)
                {
                    customer = await _invoiceRepository.GetByIdAsync(customerId.Value);
                }

                // Get all active rules ordered by priority
                var rules = await _ruleRepository.GetActiveRulesAsync();
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
                        if (Regex.IsMatch(message.ToLower(), $"\\b{Regex.Escape(keyword)}\\b"))
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        string responseText = rule.Response;

                        // Personalize the response if customer info is available
                        if (customer != null)
                        {
                            responseText = responseText
                                .Replace("{CustomerName}", customer.CustomerName ?? "Customer")
                                .Replace("{InvoiceNumber}", customer.SecondaryInvoiceID ?? "N/A")
                                .Replace("{InvoiceStatus}", customer.Stauts ?? "N/A")
                                .Replace("{MaintenanceCost}", customer.MaintenanceCost?.ToString("C") ?? "N/A")
                                .Replace("{ServiceType}", customer.ServiceType ?? "N/A")
                                .Replace("{AgreedDuration}", customer.AgreedDuration ?? "N/A")
                                .Replace("{BranchName}", customer.BranchName ?? "N/A");
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

                // If the response indicates forwarding to human, queue a follow-up
                if (response.ForwardToHuman)
                {
                    await QueueHumanFollowUpAsync(phoneNumber, message, customerId);
                }

                return ApiResponse<ChatbotResponseDTO>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatbotResponseDTO>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Queue a message for human follow-up
        /// </summary>
        public async Task<ApiResponse<bool>> QueueHumanFollowUpAsync(string phoneNumber, string message, int? customerId = null)
        {
            try
            {
                // Get customer name if available
                string customerName = "Unknown";
                if (customerId.HasValue)
                {
                    var customer = await _invoiceRepository.GetByIdAsync(customerId.Value);
                    customerName = customer?.CustomerName ?? "Unknown";
                }
                else
                {
                    var contact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);
                    customerName = contact?.Name ?? "Unknown";
                }

                // Create a notification for staff to follow up
                var notification = new Notification
                {
                    Type = "WhatsAppFollowUp",
                    ContactNumber = phoneNumber,
                    Content = $"Message from {customerName}: {message}",
                    IsHandled = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(notification);

                return ApiResponse<bool>.SuccessResponse(true, "Human follow-up queued successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all chatbot rules
        /// </summary>
        public async Task<ApiResponse<IEnumerable<ChatbotRule>>> GetRulesAsync(bool activeOnly = false)
        {
            try
            {
                IEnumerable<ChatbotRule> rules;

                if (activeOnly)
                {
                    rules = await _ruleRepository.GetActiveRulesAsync();
                }
                else
                {
                    rules = await _ruleRepository.GetAllAsync();
                }

                // Order by priority
                rules = rules.OrderByDescending(r => r.Priority).ThenBy(r => r.Name);

                return ApiResponse<IEnumerable<ChatbotRule>>.SuccessResponse(rules);
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<ChatbotRule>>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get a chatbot rule by ID
        /// </summary>
        public async Task<ApiResponse<ChatbotRule>> GetRuleAsync(int id)
        {
            try
            {
                var rule = await _ruleRepository.GetByIdAsync(id);
                if (rule == null)
                {
                    return ApiResponse<ChatbotRule>.ErrorResponse("Rule not found");
                }

                return ApiResponse<ChatbotRule>.SuccessResponse(rule);
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatbotRule>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Create a new chatbot rule
        /// </summary>
        public async Task<ApiResponse<ChatbotRule>> CreateRuleAsync(ChatbotRule rule)
        {
            try
            {
                // Validate rule
                if (string.IsNullOrWhiteSpace(rule.Name))
                {
                    return ApiResponse<ChatbotRule>.ErrorResponse("Rule name is required");
                }

                if (string.IsNullOrWhiteSpace(rule.Keywords))
                {
                    return ApiResponse<ChatbotRule>.ErrorResponse("Rule keywords are required");
                }

                if (string.IsNullOrWhiteSpace(rule.Response))
                {
                    return ApiResponse<ChatbotRule>.ErrorResponse("Rule response is required");
                }

                // Set default values
                rule.IsActive = true;

                // Save rule
                rule = await _ruleRepository.AddAsync(rule);

                return ApiResponse<ChatbotRule>.SuccessResponse(rule, "Rule created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatbotRule>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing chatbot rule
        /// </summary>
        public async Task<ApiResponse<ChatbotRule>> UpdateRuleAsync(int id, ChatbotRule rule)
        {
            try
            {
                var existingRule = await _ruleRepository.GetByIdAsync(id);
                if (existingRule == null)
                {
                    return ApiResponse<ChatbotRule>.ErrorResponse("Rule not found");
                }

                // Update properties
                existingRule.Name = rule.Name;
                existingRule.Keywords = rule.Keywords;
                existingRule.Response = rule.Response;
                existingRule.Priority = rule.Priority;
                existingRule.ForwardToHuman = rule.ForwardToHuman;

                // Save changes
                await _ruleRepository.UpdateAsync(existingRule);

                return ApiResponse<ChatbotRule>.SuccessResponse(existingRule, "Rule updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatbotRule>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete a chatbot rule
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteRuleAsync(int id)
        {
            try
            {
                var rule = await _ruleRepository.GetByIdAsync(id);
                if (rule == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Rule not found");
                }

                await _ruleRepository.DeleteAsync(rule);
                return ApiResponse<bool>.SuccessResponse(true, "Rule deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Toggle the active status of a chatbot rule
        /// </summary>
        public async Task<ApiResponse<bool>> ToggleRuleStatusAsync(int id)
        {
            try
            {
                var rule = await _ruleRepository.GetByIdAsync(id);
                if (rule == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Rule not found");
                }

                // Toggle status
                rule.IsActive = !rule.IsActive;
                await _ruleRepository.UpdateAsync(rule);

                return ApiResponse<bool>.SuccessResponse(
                    rule.IsActive,
                    $"Rule {(rule.IsActive ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Test a message against chatbot rules
        /// </summary>
        public async Task<ApiResponse<ChatbotResponseDTO>> TestRuleAsync(string message, int? customerId = null)
        {
            // Simply use the same method as processing a real message
            return await ProcessIncomingMessageAsync(message, "TEST", customerId);
        }
    }
}