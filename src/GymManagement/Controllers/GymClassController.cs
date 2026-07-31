using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using GymManagement.Application.Exceptions;
using GymManagement.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GymClassController : ControllerBase
    {
        private readonly IGymClassService _gymClassService;
        private readonly IUserService _userService;

        public GymClassController(IGymClassService gymClassService, IUserService userService)
        {
            _gymClassService = gymClassService;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult GetClasses()
        {
            var classes = _gymClassService.GetAllClasses();
            return Ok(classes);
        }

        [HttpGet("ScheduledAndSpecialClasses")]
        public IActionResult GetScheduledAndSpecialClasses()
        {
            var result = _gymClassService.GetScheduledAndSpecialClasses();
            return Ok(result);
        }

        [HttpGet("{classId}")]
        public IActionResult GetClassById(Guid classId)
        {
            var gymClass = _gymClassService.GetAdminClassById(classId);
            return Ok(gymClass);
        }

        [HttpGet("admin/{classId}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetAdminClassById(Guid classId)
        {
            var result = _gymClassService.GetAdminClassById(classId);
            return Ok(result);
        }

        [HttpPost("{classId}/join/{clientId}")]
        [Authorize]
        public IActionResult JoinClass(Guid clientId, Guid classId)
        {
            _gymClassService.JoinClass(clientId, classId, User.GetUserId(), User.GetUserRole());
            return Ok("Inscripción realizada con éxito.");
        }

        [HttpDelete("{classId}/leave/{clientId}")]
        [Authorize]
        public IActionResult LeaveClass(Guid clientId, Guid classId)
        {
            _gymClassService.LeaveClass(clientId, classId, User.GetUserId(), User.GetUserRole());
            return Ok("Inscripción eliminada correctamente.");
        }

        [HttpGet("{classId}/clients")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult GetClientsByClass(Guid classId)
        {
            var clients = _gymClassService.GetClientsByClass(classId, User.GetUserId(), User.GetUserRole());
            return Ok(clients);
        }

        [HttpGet("ClassesByTrainer/{trainerId}")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult GetTrainerClasses(Guid trainerId)
        {
            var result = _gymClassService.GetTrainerClasses(trainerId, User.GetUserId(), User.GetUserRole());
            return Ok(result);
        }

        [HttpPost("CreateClass")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateClass([FromBody] ClassRequest request)
        {
            var result = _gymClassService.CreateClass(request.TrainerId, request);
            return CreatedAtAction(nameof(GetClassById), "GymClass", new { classId = result.GymClassId }, result);
        }

        [HttpPut("{classId}")]
        [Authorize(Policy = Policies.AdminOrTrainer)]
        public IActionResult ModifyClass(Guid classId, [FromBody] ClassRequest request)
        {
            _gymClassService.ModifyClass(classId, request, User.GetUserId(), User.GetUserRole());
            return NoContent();
        }

        [HttpDelete("{classId}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult DeleteClass(Guid classId)
        {
            _gymClassService.DeleteClass(classId);
            return NoContent();
        }
    }
}
