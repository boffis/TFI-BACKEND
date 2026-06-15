using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Mappers
{
    public static class TrainerMapper
    {
        public static TrainerResponse ToTrainerResponse(this Trainer trainer)
        {
            return new TrainerResponse
            {
                UserId = trainer.UserId,
                Name = trainer.Name,
                Email = trainer.Email,
                Password = trainer.Password,
                Specialization = trainer.Specialization!
            };
        }

        public static Trainer ToTrainer(this TrainerRequest trainerRequest)
        {
            return new Trainer
            {
                UserId = Guid.NewGuid(),
                Name = trainerRequest.Name,
                Email = trainerRequest.Email,
                Password = trainerRequest.Password,
                UserRole = UserRole.Trainer,
                IsUserDeleted = false,
                Specialization = trainerRequest.Specialization
            };
        }
    }
}