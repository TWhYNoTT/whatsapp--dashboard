using Labys.Application.Services.Interfaces;
using Labys.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ConversationAssignmentController : ControllerBase
    {
        private readonly IConversationAssignmentService _assignmentService;
        private readonly IWhatsAppService _whatsAppService;

        public ConversationAssignmentController(
            IConversationAssignmentService assignmentService,
            IWhatsAppService whatsAppService)
        {
            _assignmentService = assignmentService;
            _whatsAppService = whatsAppService;
        }

        /// <summary>
        /// Get active conversations with assignment status
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveConversations(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            var response = await _whatsAppService.GetActiveConversationsAsync(page, pageSize);
            return Ok(response);
        }

        /// <summary>
        /// Get assignment details for a specific contact
        /// </summary>
        [HttpGet("{contactNumber}")]
        public async Task<IActionResult> GetAssignment(string contactNumber)
        {
            // Clean phone number
            contactNumber = contactNumber.Replace("whatsapp:", "").Trim();

            var response = await _assignmentService.GetAssignmentByContactNumberAsync(contactNumber);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Assign conversation to agent
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignConversation([FromBody] ConversationAssignmentDTO assignmentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _assignmentService.AssignConversationAsync(assignmentDto);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Release conversation (unassign)
        /// </summary>
        [HttpPost("release")]
        public async Task<IActionResult> ReleaseConversation([FromBody] ConversationReleaseDTO releaseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _assignmentService.ReleaseConversationAsync(releaseDto, currentUserId);

            if (response.Success)
                return Ok(new { Message = "Conversation released successfully" });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Toggle chatbot for a conversation
        /// </summary>
        [HttpPost("toggle-chatbot")]
        public async Task<IActionResult> ToggleChatbot([FromBody] ChatbotToggleDTO toggleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _assignmentService.ToggleChatbotAsync(toggleDto, currentUserId);

            if (response.Success)
                return Ok(new
                {
                    ContactNumber = toggleDto.ContactNumber,
                    ChatbotDisabled = toggleDto.DisableChatbot
                });

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Get assigned conversations for current agent
        /// </summary>
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _assignmentService.GetUserAssignmentsAsync(currentUserId);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Update agent activity for a conversation
        /// </summary>
        [HttpPost("update-activity")]
        public async Task<IActionResult> UpdateActivity([FromBody] ActivityUpdateDTO activityDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _assignmentService.UpdateActivityAsync(activityDto, currentUserId);

            if (response.Success)
                return Ok(new
                {
                    ContactNumber = activityDto.ContactNumber,
                    ActivityUpdated = true
                });

            return BadRequest(new { Error = response.Message });
        }
    }
}