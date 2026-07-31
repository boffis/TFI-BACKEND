namespace GymManagement.Application.Responses
{
    public class ActiveTrainerResponse
    {
        public required string Name { get; set; }

        public string? Specialization { get; set; }

        public Guid UserId { get; set; }
    }
}
