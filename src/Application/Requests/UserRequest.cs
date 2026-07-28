namespace GymManagement.Application.Requests
{
    public class UserRequest
    {
        public required string Name { get; set; }

        public required string Email { get; set; }
        
        public required string Password { get; set; }

        public DateOnly DateOfBirth { get; set; }

        public required string DNI { get; set; }

        public required string Gender { get; set; }

        public required string PhoneNumber { get; set; }
    }
}   