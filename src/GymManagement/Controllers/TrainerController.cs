using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class TrainerController : ControllerBase
    {
        private readonly TrainerService _trainerService;

        public TrainerController(TrainerService trainerService)
        {
            _trainerService = trainerService;
        }

        [HttpGet("Trainers")]
        public IActionResult GetAllTrainers()
        {
            var trainers = _trainerService.GetAllTrainers();
            return Ok(trainers);
        }

        [HttpGet("{userId}")]
        public ActionResult<TrainerResponse> GetTrainerById(Guid userId)
        {
            var trainer = _trainerService.GetTrainerById(userId);
            if (trainer == null)
            {
                return NotFound();
            }
            return Ok(trainer);
        }

        [HttpPut("{userId}")]
        public ActionResult<TrainerResponse> UpdateTrainer(Guid userId, [FromBody] TrainerRequest request)
        {
            var trainer = _trainerService.GetTrainerById(userId);
            if (trainer == null)
            {
                return NotFound();
            }

            var updatedTrainer = _trainerService.UpdateTrainer(userId, request);
            return Ok(updatedTrainer);
        }

        [HttpGet("Classes/MyClasses")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult GetTrainerClasses()
        {
            var trainerId = VerifyIfUserCreateTheClass();
            var result = _trainerService.GetTrainerClasses(trainerId);
            return Ok(result);
        }

        [HttpPost("Classes")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult CreateClass([FromBody] ClassRequest request)
        {
            var trainerId = VerifyIfUserCreateTheClass();
            var trainer = _trainerService.GetTrainerById(trainerId);

            if (trainer == null)
            {
                return NotFound("Entrenador no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(trainer.Specialization))
            {
                return BadRequest("El entrenador debe tener una especialización.");
            }

            var result = _trainerService.CreateClass(trainerId, request);

            return CreatedAtAction(
                nameof(GetTrainerById),
                new { userId = trainerId },
                result
            );
        }

        [HttpPut("Classes/{classId}")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult ModifyTrainerClasses(Guid classId, [FromBody] ClassRequest request)
        {
            var trainerId = VerifyIfUserCreateTheClass();
            var success = _trainerService.ModifyTrainerClasses(trainerId, classId, request);
            if (!success)
            {
                return NotFound("Clase no encontrada o no pertenece a este entrenador.");
            }
            return NoContent();
        }

        [HttpDelete("Classes/{classId}")]
        [Authorize(Policy = Policies.OnlyTrainer)]
        public IActionResult DeleteTrainerClasses(Guid classId)
        {
            var trainerId = VerifyIfUserCreateTheClass();
            var success = _trainerService.DeleteTrainerClasses(trainerId, classId);
            if (!success)
            {
                return NotFound("Clase no encontrada o no pertenece a este entrenador.");
            }
            return NoContent();
        }

        private Guid VerifyIfUserCreateTheClass()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                throw new UnauthorizedAccessException("Usuario no autenticado");
            return Guid.Parse(claim.Value);
        }
    }
}