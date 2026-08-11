using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    /// <summary>
    /// Aggregate queries behind the admin metrics dashboard.
    /// <para>
    /// Every method here must aggregate in the database rather than materialising rows and counting
    /// them in memory. A reporting screen is exactly where a naive implementation quietly loads
    /// every payment and inscription in the gym.
    /// </para>
    /// </summary>
    public interface IMetricsRepository
    {
        /// <summary>All-time value of payments in the given states.</summary>
        Task<(decimal Total, int Count)> GetRevenueTotalAsync(IReadOnlyCollection<string> revenueStates);

        /// <summary>
        /// Date, amount and state of every payment since <paramref name="utcFrom"/>. Returned as rows
        /// rather than pre-grouped because calendar months depend on the gym's time zone, which SQL
        /// Server has no way to apply — the grouping happens in the service. The window is bounded,
        /// so this is one query over a small slice, not a full table load.
        /// </summary>
        Task<List<PaymentPoint>> GetPaymentsSinceAsync(DateTime utcFrom);

        /// <summary>Count and value of payments grouped by their raw state string.</summary>
        Task<List<PaymentStateResponse>> GetPaymentStateBreakdownAsync();

        Task<MemberCounts> GetMemberCountsAsync(DateTime utcNow);

        Task<List<PlanMetricsResponse>> GetPlanBreakdownAsync(IReadOnlyCollection<string> revenueStates);

        /// <summary>
        /// Class totals. Bounds are gym wall-clock values, matching how <c>GymClass.Schedule</c> is stored.
        /// </summary>
        Task<ClassCounts> GetClassCountsAsync(DateTime windowStart, DateTime now);

        Task<List<PopularClassResponse>> GetPopularClassesAsync(DateTime windowStart, DateTime now, int take);

        Task<AttendanceCounts> GetAttendanceCountsAsync(DateTime windowStart, DateTime now);

        Task<List<TrainerMetricsResponse>> GetTrainerBreakdownAsync(DateTime windowStart, DateTime now);
    }

    public record PaymentPoint(DateTime PaymentDate, decimal Price, string PaymentState);

    public record MemberCounts(int TotalClients, int WithActiveMembership, int Expired, int Cancelled);

    public record ClassCounts(int Upcoming, int HeldInWindow, int InscriptionsInWindow, int CapacityInWindow);

    public record AttendanceCounts(int Present, int Absent, int NotRecorded);
}
