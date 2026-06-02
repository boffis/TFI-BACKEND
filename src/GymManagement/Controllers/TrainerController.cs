using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
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

        [HttpGet("Trainer/{userId}")]
        public ActionResult<TrainerResponse> GetTrainerById(Guid userId)
        {
            var trainer = _trainerService.GetTrainerById(userId);
            if (trainer == null)
            {
                return NotFound();
            }
            return Ok(trainer);
        }
    }
}