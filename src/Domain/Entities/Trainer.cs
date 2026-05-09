namespace GymManagement.Domain.Entities
{
    public class Trainer : User
    {
        public string? Specialization { get; set; }

        public ICollection<GymClass> GymClasses { get; set; } = new List<GymClass>();
    }
}
