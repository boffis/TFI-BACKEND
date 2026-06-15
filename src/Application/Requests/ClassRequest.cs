using System;

namespace GymManagement.Application.Requests
{
    public class ClassRequest
    {
        public required string ClassName { get; set; }
        public string? ClassDescription { get; set; }
        public required int MaxCapacity { get; set; }
        public required DateTime Schedule { get; set; }
    }
}
