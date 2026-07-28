using GymManagement.Domain.Enums;

namespace GymManagement.Domain.Entities
{
    public abstract class User
    {
        public Guid UserId { get; set; }
  
        public required string Name { get; set; }
   
        public required string Email { get; set; }
   
        public required string Password { get; set; }

        public DateOnly DateOfBirth { get; set; }

        public required string DNI { get; set; }

        public required string Gender { get; set; }

        public required string PhoneNumber { get; set; }
     
        public bool IsUserDeleted { get; set; } = false;

        public bool IsEmailConfirmed { get; set; } = false;

        public string? EmailConfirmationToken { get; set; }

        public DateTime? EmailConfirmationTokenExpiration { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiration { get; set; }
    }
}