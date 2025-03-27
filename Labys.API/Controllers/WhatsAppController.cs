using Labys.Application.Services.Interfaces;
using Labys.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly IContactService _contactService;

        public WhatsAppController(
            IWhatsAppService whatsAppService,
            IContactService contactService)
        {
            _whatsAppService = whatsAppService;
            _contactService = contactService;
        }

        /// <summary>
        /// Send a WhatsApp message with tracking
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] WhatsAppMessageDTO message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _whatsAppService.SendMessageAsync(message, userId);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Send a template message
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("send-template")]
        public async Task<IActionResult> SendTemplateMessage([FromBody] WhatsAppTemplateDTO template)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _whatsAppService.SendTemplateMessageAsync(template, userId);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Webhook for receiving WhatsApp messages
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveMessage()
        {
            try
            {
                // Extract message data from the request
                var form = await Request.ReadFormAsync();
                var formData = new Dictionary<string, string>();

                foreach (var key in form.Keys)
                {
                    formData[key] = form[key];
                }

                var response = await _whatsAppService.ProcessWebhookAsync(formData);
                return Content(response.Response, response.ResponseType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error processing webhook", Details = ex.Message });
            }
        }

        /// <summary>
        /// Webhook for status callbacks
        /// </summary>
        
        [HttpPost("status")]
        public async Task<IActionResult> MessageStatus()
        {
            try
            {
                // Create a safer way to get form data
                var formData = new Dictionary<string, string>();
                try
                {
                    var form = await Request.ReadFormAsync();
                    foreach (var key in form.Keys)
                    {
                        formData[key] = form[key];
                    }
                }
                catch
                {
                    // If form reading fails, try to read from the raw request body
                    Request.Body.Position = 0;
                    using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                    var rawBody = await reader.ReadToEndAsync();
                    Request.Body.Position = 0;
                    // Try to parse the parameters from the raw body
                    var bodyParts = rawBody.Split('&');
                    foreach (var part in bodyParts)
                    {
                        var keyValue = part.Split('=');
                        if (keyValue.Length == 2)
                        {
                            var key = Uri.UnescapeDataString(keyValue[0]);
                            var value = Uri.UnescapeDataString(keyValue[1]);
                            formData[key] = value;
                        }
                    }
                }
                var response = await _whatsAppService.ProcessStatusCallbackAsync(formData);

                // Change this line:
                return Content("OK", "text/plain");
            }
            catch (Exception)
            {
                // Also change this line:
                return Content("Error processed", "text/plain");
            }
        }

        /// <summary>
        /// Fetch message history for a contact
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("history/{phoneNumber}")]
        public async Task<IActionResult> GetMessageHistory(string phoneNumber, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Clean phone number
            phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

            var response = await _whatsAppService.GetMessageHistoryAsync(phoneNumber, page, pageSize);

            if (response.Success)
                return Ok(response);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Get all active conversations
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetActiveConversations(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var response = await _whatsAppService.GetActiveConversationsAsync(page, pageSize);

            if (response.Success)
                return Ok(response);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Send a file or media message
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("send-media")]
        public async Task<IActionResult> SendMediaMessage([FromForm] WhatsAppMediaMessageDTO message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var response = await _whatsAppService.SendMediaMessageAsync(message, userId, baseUrl);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// API health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "OK",
                Message = "WhatsApp API is operational",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test webhook validation (for Twilio setup)
        /// </summary>
        [HttpPost("test-webhook")]
        public IActionResult TestWebhook()
        {
            return Content(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response><Message>Webhook test successful! Your WhatsApp integration is working correctly.</Message></Response>",
                "application/xml");
        }
    }
}