using GymManagement.Domain.Enums;

namespace GymManagement.Domain.Entities
{
    public abstract class User
    {
        public Guid UserId { get; set; }
  
        public required string Name { get; set; }
   
        public required string Email { get; set; }
   
        public required string Password { get; set; }
      
        public UserRole UserRole { get; set; } = UserRole.Client;
     
        public bool IsUserDeleted { get; set; } = false;             
    }
}