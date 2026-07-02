using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Presentation.Controllers
{
    public class GymClassController : ControllerBase
    {
        private readonly GymClassService _gymClassService;
        private readonly TrainerService _trainerService;

        public GymClassController(GymClassService gymClassService, TrainerService trainerService)
        {
            _gymClassService = gymClassService;
            _trainerService = trainerService;
        }

        [HttpGet("Classes")]
        public IActionResult GetClasses()
        {
            var classes = _gymClassService.GetAllClasses();
            return Ok(classes);
        }

        [HttpGet("Classes/{classId}")]
        public IActionResult GetClassById(Guid classId)
        {
            var gymClass = _gymClassService.GetClassById(classId);
            if (gymClass == null) 
                return NotFound();
            return Ok(gymClass);
        }

        [HttpPost("{classId}/join/{clientId}")]
        public IActionResult JoinClass(Guid clientId, Guid classId)
        {
            var result = _gymClassService.JoinClass(clientId, classId);
            if (!result) return BadRequest("No se pudo inscribir: clase llena o cliente ya inscripto.");
            return Ok("Inscripción realizada con éxito.");
        }

        [HttpDelete("{classId}/leave/{clientId}")]
        public IActionResult LeaveClass(Guid clientId, Guid classId)
        {
            var result = _gymClassService.LeaveClass(clientId, classId);
            if (!result) return BadRequest("El cliente no estaba inscripto en esta clase.");
            return Ok("Inscripción eliminada correctamente.");
        }

        [HttpGet("{classId}/clients")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetClientsByClass(Guid classId)
        {
            var clients = _gymClassService.GetClientsByClass(classId);
            return Ok(clients);
        }

        [HttpGet("Classes/MyClasses")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult GetTrainerClasses()
        {
            var trainerId = VerifyIfThisTrainerCreatedTheClass();
            var result = _gymClassService.GetTrainerClasses(trainerId);
            return Ok(result);
        }

        [HttpPost("Classes/CreateClass")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult CreateClass([FromBody] ClassRequest request)
        {
            var trainerId = VerifyIfThisTrainerCreatedTheClass();
            var trainer = _trainerService.GetTrainerById(trainerId);
            if (trainer == null)
                return NotFound("Entrenador no encontrado.");
            if (string.IsNullOrWhiteSpace(trainer.Specialization))
                return BadRequest("El entrenador debe tener una especialización.");
            var result = _gymClassService.CreateClass(trainerId, request);
            return CreatedAtAction(nameof(GetClassById),"GymClass", new { classId = result.GymClassId },result);
        }

        [HttpPut("Classes/{classId}")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult ModifyTrainerClasses(Guid classId, [FromBody] ClassRequest request)
        {
            var trainerId = VerifyIfThisTrainerCreatedTheClass();
            var success = _gymClassService.ModifyTrainerClasses(trainerId, classId, request);
            if (!success) 
                return NotFound("Clase no encontrada o no pertenece a este entrenador.");
            return NoContent();
        }

        [HttpDelete("Classes/{classId}")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult DeleteTrainerClasses(Guid classId)
        {
            var trainerId = VerifyIfThisTrainerCreatedTheClass();
            var success = _gymClassService.DeleteTrainerClasses(trainerId, classId);
            if (!success) 
                return NotFound("Clase no encontrada o no pertenece a este entrenador.");
            return NoContent();
        }

        private Guid VerifyIfThisTrainerCreatedTheClass()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                throw new UnauthorizedAccessException("Usuario no autenticado");
            return Guid.Parse(claim.Value);
        }
    }
}