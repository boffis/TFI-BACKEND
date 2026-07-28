using System;
using System.Security.Claims;
using GymManagement.Application.Exceptions;
using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using GymManagement.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymClassScheduleController : ControllerBase
    {
        private readonly GymClassScheduleService _scheduleService;

        public GymClassScheduleController(GymClassScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet]
        public IActionResult GetAllSchedules()
        {
            var schedules = _scheduleService.GetAllSchedules();
            return Ok(schedules);
        }

        [HttpGet("{id}")]
        public IActionResult GetScheduleById(Guid id)
        {
            var schedule = _scheduleService.GetScheduleById(id);
            if (schedule == null) return NotFound();
            return Ok(schedule);
        }

        [HttpPost]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult CreateSchedule([FromBody] GymClassScheduleRequest request)
        {
            var trainerId = User.GetUserId();
            var result = _scheduleService.CreateSchedule(trainerId, request);
            return CreatedAtAction(nameof(GetScheduleById), new { id = result.GymClassScheduleId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult ModifySchedule(Guid id, [FromBody] GymClassScheduleRequest request)
        {
            var success = _scheduleService.ModifySchedule(id, request);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult DeleteSchedule(Guid id)
        {
            var success = _scheduleService.DeleteSchedule(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("generate")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult GenerateUpcomingSessions([FromQuery] int weeksAhead = 2)
        {
            var createdClasses = _scheduleService.GenerateUpcomingSessions(weeksAhead);
            return Ok(createdClasses);
        }
    }
}
