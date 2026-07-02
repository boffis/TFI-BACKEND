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
    [Authorize]
    [ApiController]
    public class TrainerController : ControllerBase
    {
        private readonly TrainerService _trainerService;
        private readonly GymClassService _gymClassService;

        public TrainerController(TrainerService trainerService, GymClassService gymClassService)
        {
            _trainerService = trainerService;
            _gymClassService = gymClassService;
        }

        [HttpGet("Trainers")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetTrainers()
        {
            var trainers = _trainerService.GetAllTrainers();
            return Ok(trainers);
        }

        [HttpGet("{userId}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public ActionResult<TrainerResponse> GetTrainerById(Guid userId)
        {
            var trainer = _trainerService.GetTrainerById(userId);
            if (trainer == null) return NotFound();
            return Ok(trainer);
        }

        [HttpPut("{userId}")]
        [Authorize]
        public ActionResult<TrainerResponse> UpdateTrainer(Guid userId, [FromBody] TrainerRequest request)
        {
            var trainer = _trainerService.GetTrainerById(userId);
            if (trainer == null) return NotFound();
            var updatedTrainer = _trainerService.UpdateTrainer(userId, request);
            return Ok(updatedTrainer);
        }
    }
}