using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    /// <summary>
    /// Aggregate queries behind the admin metrics dashboard. Every method must aggregate in the
    /// database, not by materialising rows and counting them in memory.
    /// </summary>
    public interface IMetricsRepository
    {
        /// <summary>All-time value of payments in the given states.</summary>
        Task<(decimal Total, int Count)> GetRevenueTotalAsync(IReadOnlyCollection<string> revenueStates);

        /// <summary>
        /// Every payment since <paramref name="utcFrom"/>, ungrouped: calendar months depend on the
        /// gym's time zone, which SQL Server can't apply, so the service groups them.
        /// </summary>
        Task<List<PaymentPoint>> GetPaymentsSinceAsync(DateTime utcFrom);

        /// <summary>Count and value of payments grouped by their raw state string.</summary>
        Task<List<PaymentStateResponse>> GetPaymentStateBreakdownAsync();

        Task<MemberCounts> GetMemberCountsAsync(DateTime utcNow);

        Task<List<PlanMetricsResponse>> GetPlanBreakdownAsync(IReadOnlyCollection<string> revenueStates);

        /// <summary>Class totals. Bounds are gym wall-clock, matching <c>GymClass.Schedule</c>.</summary>
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
