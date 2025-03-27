using Labys.Application.Services.Interfaces;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly IContactService _contactService;

        public CampaignsController(
            ICampaignService campaignService,
            IContactService contactService)
        {
            _campaignService = campaignService;
            _contactService = contactService;
        }

        /// <summary>
        /// Get all campaigns with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCampaigns(
            int page = 1,
            int pageSize = 10,
            string status = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var response = await _campaignService.GetCampaignsAsync(page, pageSize, status);
            return Ok(response);
        }

        /// <summary>
        /// Get campaign details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampaign(int id)
        {
            var response = await _campaignService.GetCampaignAsync(id);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Create a new campaign
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCampaign([FromBody] CampaignDTO campaignDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var campaign = new WhatsAppCampaign
            {
                Name = campaignDto.Name,
                Description = campaignDto.Description,
                TemplateSid = campaignDto.TemplateSid,
                ScheduledDate = campaignDto.ScheduledDate,
                AudienceFilter = campaignDto.AudienceFilter,
                Variable1 = campaignDto.Variable1,
                Variable2 = campaignDto.Variable2,
                Variable3 = campaignDto.Variable3
            };

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _campaignService.CreateCampaignAsync(campaign, campaignDto.ContactIds, userId);

            if (response.Success)
                return CreatedAtAction(nameof(GetCampaign), new { id = response.Data.Id }, response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Update a campaign
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] CampaignDTO campaignDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var campaign = new WhatsAppCampaign
            {
                Name = campaignDto.Name,
                Description = campaignDto.Description,
                TemplateSid = campaignDto.TemplateSid,
                ScheduledDate = campaignDto.ScheduledDate,
                AudienceFilter = campaignDto.AudienceFilter,
                Variable1 = campaignDto.Variable1,
                Variable2 = campaignDto.Variable2,
                Variable3 = campaignDto.Variable3
            };

            var response = await _campaignService.UpdateCampaignAsync(id, campaign, campaignDto.ContactIds);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Launch a campaign
        /// </summary>
        [HttpPost("{id}/launch")]
        public async Task<IActionResult> LaunchCampaign(int id)
        {
            var response = await _campaignService.LaunchCampaignAsync(id);

            if (response.Success)
                return Ok(new { Message = "Campaign launched successfully", CampaignId = id });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Cancel a campaign
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelCampaign(int id)
        {
            var response = await _campaignService.CancelCampaignAsync(id);

            if (response.Success)
                return Ok(new { Message = "Campaign cancelled successfully", CampaignId = id });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Delete a campaign
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var response = await _campaignService.DeleteCampaignAsync(id);

            if (response.Success)
                return NoContent();

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Get campaign analytics
        /// </summary>
        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> GetCampaignAnalytics(int id)
        {
            var response = await _campaignService.GetCampaignAnalyticsAsync(id);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Preview a campaign message
        /// </summary>
        [HttpPost("preview")]
        public IActionResult PreviewCampaign([FromBody] CampaignPreviewDTO previewDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // In a real implementation, this would preview the actual template
            // For now, we'll just return a simplified preview
            var variables = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(previewDto.Variable1)) variables["1"] = previewDto.Variable1;
            if (!string.IsNullOrEmpty(previewDto.Variable2)) variables["2"] = previewDto.Variable2;
            if (!string.IsNullOrEmpty(previewDto.Variable3)) variables["3"] = previewDto.Variable3;

            return Ok(new
            {
                TemplateSid = previewDto.TemplateSid,
                Variables = variables,
                PreviewNote = "This is a preview of how the message will appear with your variables."
            });
        }
    }
}