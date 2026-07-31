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
    [Route("[controller]")]
    [ApiController]
    public class GymClassScheduleController : ControllerBase
    {
        private readonly GymClassScheduleService _scheduleService;

        public GymClassScheduleController(GymClassScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult GetAllSchedules()
        {
            var role = User.GetUserRole();
            var schedules = role == "Admin" 
                ? _scheduleService.GetAllSchedules() 
                : _scheduleService.GetSchedulesByTrainerId(User.GetUserId());
            return Ok(schedules);
        }

        [HttpGet("{id}")]
        public IActionResult GetScheduleById(Guid id)
        {
            var schedule = _scheduleService.GetAdminScheduleById(id);
            return Ok(schedule);
        }

        [HttpGet("admin/{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetAdminScheduleById(Guid id)
        {
            var result = _scheduleService.GetAdminScheduleById(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateSchedule([FromBody] GymClassScheduleRequest request)
        {
            var result = _scheduleService.CreateSchedule(request.TrainerId, request);
            return CreatedAtAction(nameof(GetScheduleById), new { id = result.GymClassScheduleId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult ModifySchedule(Guid id, [FromBody] GymClassScheduleRequest request, [FromQuery] bool updateUpcomingClasses = false)
        {
            _scheduleService.ModifySchedule(id, request, User.GetUserId(), User.GetUserRole(), updateUpcomingClasses);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult DeleteSchedule(Guid id, [FromQuery] bool deleteUpcomingClasses = false)
        {
            _scheduleService.DeleteSchedule(id, deleteUpcomingClasses);
            return NoContent();
        }

        [HttpPost("generate")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult GenerateUpcomingSessions([FromQuery] int daysAhead = 14)
        {
            var createdClasses = _scheduleService.GenerateUpcomingSessions(daysAhead);
            return Ok(createdClasses);
        }
    }
}
