namespace GymManagement.Application.Requests
{
    public class UserRequest
    {
        public required string Name { get; set; }

        public required string Email { get; set; }
        
        public required string Password { get; set; }
    }
}
