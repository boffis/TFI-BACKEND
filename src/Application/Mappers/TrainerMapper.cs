using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

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
                DateOfBirth = trainer.DateOfBirth,
                DNI = trainer.DNI,
                Gender = trainer.Gender,
                PhoneNumber = trainer.PhoneNumber,
                Role = trainer.GetType().Name,
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
                DateOfBirth = trainerRequest.DateOfBirth,
                DNI = trainerRequest.DNI,
                Gender = trainerRequest.Gender,
                PhoneNumber = trainerRequest.PhoneNumber,
                IsUserDeleted = false,
                Specialization = trainerRequest.Specialization
            };
        }
    }
}
