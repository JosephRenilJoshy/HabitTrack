using HabitService.DTOs;
using HabitService.Filters;
using HabitService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HabitService.Controllers
{
    [ApiController]
    [Route("api/habits")]
    [Authorize]
    public class HabitsController : ControllerBase
    {
        private readonly IHabitService _habitService;

        public HabitsController(IHabitService habitService)
        {
            _habitService = habitService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return int.Parse(userIdClaim!);
        }

        [HttpPost]
        public async Task<ActionResult<HabitResponseDto>> Create([FromBody] CreateHabitDto dto)
        {
            var result = await _habitService.CreateAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<ActionResult<List<HabitResponseDto>>> GetAll()
        {
            var result = await _habitService.GetByUserIdAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HabitResponseDto>> GetById(int id)
        {
            var result = await _habitService.GetByIdAsync(id, GetUserId());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _habitService.DeleteAsync(id, GetUserId());
            return NoContent();
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInDto dto)
        {
            await _habitService.CheckInAsync(id, GetUserId(), dto);
            return NoContent();
        }

        [HttpGet("{id}/strek")]
        [ServiceFilter(typeof(StreakCacheResourceFilter))] // Apply the resource filter to this action
        public async Task<ActionResult<StreakResponseDto>> GetStreak(int id)
        {
            var result = await _habitService.GetStreakAsync(id, GetUserId());
            return Ok(result);
        }

        [HttpGet("habit-with-count")]
        public async Task<ActionResult<List<HabitWithCountDto>>> GetWithCountsBad()
        {
            var result = await _habitService.GetHabitWithCountsBadAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("habit-with-count-optimized")]
        public async Task<ActionResult<List<HabitWithCountDto>>> GetWithCountsOptimized()
        {
            var result = await _habitService.GetHabitWithCountsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("internal/at-risk")]
        [AllowAnonymous]
        [ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
        public async Task<ActionResult<List<HabitResponseDto>>> GetAtRiskHabitsForAllUsers()
        {
            // This is an internal endpoint that is called by the NotificationService to get
            // a list of habits that are at risk of being missed, so that it
            // can send notifications to users about their habits.
            var result = await _habitService.GetAtRiskHabitsAsync();
            return Ok(result);
        }
    }
}
