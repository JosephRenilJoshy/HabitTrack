using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Filters;
using NotificationService.Services;
using System.Security.Claims;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            var userId = int.Parse(userIdClaim!);

            var result = await _notificationService.GetForUserAsync(userId);
            return Ok(result);
        }

        [HttpPost("milestone")]
        [AllowAnonymous]
        [ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
        public async Task<IActionResult> ReceiveMilestone([FromBody] MilestoneNotificationDto dto)
        {
            await _notificationService.CreateMilestoneNotificationAsync(dto.UserId, dto.HabitId, dto.HabitName, dto.Milestone);
            return NoContent();
        }
    }
}
