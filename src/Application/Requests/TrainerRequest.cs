namespace GymManagement.Application.Requests
{
    public class TrainerRequest : UserRequest
    {
        public required string Specialization { get; set; }
    }
}