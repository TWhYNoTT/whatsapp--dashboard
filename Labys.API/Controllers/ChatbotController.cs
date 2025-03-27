using Labys.Application.Services.Interfaces;
using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        /// <summary>
        /// Get all chatbot rules
        /// </summary>
        [HttpGet("rules")]
        public async Task<IActionResult> GetRules(bool activeOnly = false)
        {
            var response = await _chatbotService.GetRulesAsync(activeOnly);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Get a specific rule
        /// </summary>
        [HttpGet("rules/{id}")]
        public async Task<IActionResult> GetRule(int id)
        {
            var response = await _chatbotService.GetRuleAsync(id);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Create a new rule
        /// </summary>
        [HttpPost("rules")]
        public async Task<IActionResult> CreateRule([FromBody] ChatbotRuleDTO ruleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rule = new ChatbotRule
            {
                Name = ruleDto.Name,
                Keywords = ruleDto.Keywords,
                Response = ruleDto.Response,
                Priority = ruleDto.Priority,
                ForwardToHuman = ruleDto.ForwardToHuman,
                IsActive = true
            };

            var response = await _chatbotService.CreateRuleAsync(rule);

            if (response.Success)
                return CreatedAtAction(nameof(GetRule), new { id = response.Data.Id }, response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Update a rule
        /// </summary>
        [HttpPut("rules/{id}")]
        public async Task<IActionResult> UpdateRule(int id, [FromBody] ChatbotRuleDTO ruleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rule = new ChatbotRule
            {
                Name = ruleDto.Name,
                Keywords = ruleDto.Keywords,
                Response = ruleDto.Response,
                Priority = ruleDto.Priority,
                ForwardToHuman = ruleDto.ForwardToHuman
            };

            var response = await _chatbotService.UpdateRuleAsync(id, rule);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Toggle rule status (active/inactive)
        /// </summary>
        [HttpPatch("rules/{id}/toggle-status")]
        public async Task<IActionResult> ToggleRuleStatus(int id)
        {
            var response = await _chatbotService.ToggleRuleStatusAsync(id);

            if (response.Success)
                return Ok(new { RuleId = id, IsActive = response.Data });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Delete a rule
        /// </summary>
        [HttpDelete("rules/{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var response = await _chatbotService.DeleteRuleAsync(id);

            if (response.Success)
                return NoContent();

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Test a message against chatbot rules
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestMessage([FromBody] ChatbotTestDTO testDto)
        {
            if (string.IsNullOrWhiteSpace(testDto.Message))
            {
                return BadRequest(new { Error = "Message cannot be empty." });
            }

            var response = await _chatbotService.TestRuleAsync(testDto.Message, testDto.CustomerId);

            if (response.Success)
            {
                return Ok(new
                {
                    InputMessage = testDto.Message,
                    Response = response.Data.Message,
                    MatchedRuleId = response.Data.MatchedRuleId,
                    ForwardToHuman = response.Data.ForwardToHuman
                });
            }

            return BadRequest(new { Error = response.Message });
        }
    }
}