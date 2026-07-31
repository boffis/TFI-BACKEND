namespace GymManagement.Application.Responses
{
    /// <summary>
    /// Lightweight trainer info suitable for public-facing endpoints.
    /// </summary>
    public class TrainerSummaryResponse
    {
        public Guid TrainerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Specialization { get; set; }
    }
}
