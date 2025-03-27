using Labys.Application.Services.Interfaces;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateService _templateService;

        public TemplatesController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        /// <summary>
        /// Get all templates with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTemplates(bool approvedOnly = false, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var response = await _templateService.GetTemplatesAsync(page, pageSize, approvedOnly);
            return Ok(response);
        }

        /// <summary>
        /// Get a template by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(int id)
        {
            var response = await _templateService.GetTemplateAsync(id);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Get a template by Content SID
        /// </summary>
        [HttpGet("sid/{contentSid}")]
        public async Task<IActionResult> GetTemplateBySid(string contentSid)
        {
            var response = await _templateService.GetTemplateByContentSidAsync(contentSid);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Create a new template
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] WhatsAppTemplateCreationDTO templateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = new WhatsAppTemplate
            {
                Name = templateDto.Name,
                Description = templateDto.Description,
                ContentSid = templateDto.ContentSid,
                Language = templateDto.Language ?? "en",
                Type = templateDto.Type
            };

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _templateService.CreateTemplateAsync(template, userId);

            if (response.Success)
                return CreatedAtAction(nameof(GetTemplate), new { id = response.Data.Id }, response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Update a template
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] WhatsAppTemplateCreationDTO templateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = new WhatsAppTemplate
            {
                Name = templateDto.Name,
                Description = templateDto.Description,
                ContentSid = templateDto.ContentSid,
                Language = templateDto.Language ?? "en",
                Type = templateDto.Type
            };

            var response = await _templateService.UpdateTemplateAsync(id, template);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Delete a template
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var response = await _templateService.DeleteTemplateAsync(id);

            if (response.Success)
                return NoContent();

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Update approval status
        /// </summary>
        [HttpPatch("{id}/approval")]
        [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can approve templates
        public async Task<IActionResult> UpdateApprovalStatus(int id, [FromBody] TemplateApprovalDTO approvalDto)
        {
            var response = await _templateService.UpdateApprovalStatusAsync(id, approvalDto.IsApproved);

            if (response.Success)
                return Ok(new { TemplateId = id, IsApproved = approvalDto.IsApproved });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Test a template
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestTemplate([FromBody] TemplateTestDTO testDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create variables list
            var variables = new List<string>();
            if (!string.IsNullOrEmpty(testDto.Variable1)) variables.Add(testDto.Variable1);
            if (!string.IsNullOrEmpty(testDto.Variable2)) variables.Add(testDto.Variable2);
            if (!string.IsNullOrEmpty(testDto.Variable3)) variables.Add(testDto.Variable3);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = await _templateService.TestTemplateAsync(
                testDto.TemplateSid,
                testDto.ToNumber,
                variables,
                userId);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Get template categories for filtering
        /// </summary>
        [HttpGet("categories")]
        public IActionResult GetTemplateCategories()
        {
            // Common WhatsApp template categories
            var categories = new List<string>
            {
                "marketing",
                "utility",
                "authentication",
                "account_update",
                "payment_update",
                "personal_finance",
                "shipping_update",
                "reservation_update",
                "issue_resolution",
                "appointment_update",
                "auto_reply",
                "customer_feedback"
            };

            return Ok(categories);
        }
    }
}