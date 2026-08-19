using System.Globalization;
using GymManagement.Application.Common;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Services
{
    /// <summary>Assembles the admin metrics dashboard.</summary>
    public class MetricsService
    {
        /// <summary>
        /// Payment states that count as money received. Everything else Mercado Pago reports
        /// (pending, rejected, refunded…) is surfaced in <see cref="RevenueMetricsResponse.ByState"/>
        /// so a low total can be explained rather than guessed at.
        /// </summary>
        private static readonly string[] RevenueStates = ["approved"];

        /// <summary>Window used by the class, attendance and trainer sections.</summary>
        private const int RecentWindowDays = 30;

        private const int MonthsOfHistory = 6;
        private const int PopularClassCount = 5;

        private readonly IMetricsRepository _repository;

        public MetricsService(IMetricsRepository repository)
        {
            _repository = repository;
        }

        public async Task<MetricsResponse> GetMetricsAsync()
        {
            // Classes are gym wall-clock, payments are UTC — each window uses its own clock.
            var gymNow = GymTime.Now;
            var classWindowStart = gymNow.AddDays(-RecentWindowDays);

            var firstMonthStart = new DateTime(gymNow.Year, gymNow.Month, 1).AddMonths(-(MonthsOfHistory - 1));

            var (total, paidCount) = await _repository.GetRevenueTotalAsync(RevenueStates);
            var recentPayments = await _repository.GetPaymentsSinceAsync(GymTime.ToUtc(firstMonthStart));
            var byState = await _repository.GetPaymentStateBreakdownAsync();
            var members = await _repository.GetMemberCountsAsync(DateTime.UtcNow);
            var plans = await _repository.GetPlanBreakdownAsync(RevenueStates);
            var classCounts = await _repository.GetClassCountsAsync(classWindowStart, gymNow);
            var popular = await _repository.GetPopularClassesAsync(classWindowStart, gymNow, PopularClassCount);
            var attendance = await _repository.GetAttendanceCountsAsync(classWindowStart, gymNow);
            var trainers = await _repository.GetTrainerBreakdownAsync(classWindowStart, gymNow);

            foreach (var state in byState)
                state.CountsAsRevenue = IsRevenue(state.State);

            var byMonth = GroupByGymMonth(recentPayments, firstMonthStart, gymNow);
            var thisMonth = byMonth.LastOrDefault(m => m.Year == gymNow.Year && m.Month == gymNow.Month);

            var marked = attendance.Present + attendance.Absent;
            var allEnrolments = marked + attendance.NotRecorded;

            return new MetricsResponse
            {
                RecentWindowDays = RecentWindowDays,
                RevenueStates = [.. RevenueStates],
                Revenue = new RevenueMetricsResponse
                {
                    Total = total,
                    PaidPaymentCount = paidCount,
                    ThisMonth = thisMonth?.Amount ?? 0m,
                    ByMonth = byMonth,
                    ByState = byState
                },
                Members = new MemberMetricsResponse
                {
                    TotalClients = members.TotalClients,
                    WithActiveMembership = members.WithActiveMembership,
                    Expired = members.Expired,
                    Cancelled = members.Cancelled
                },
                Plans = plans,
                Classes = new ClassMetricsResponse
                {
                    Upcoming = classCounts.Upcoming,
                    HeldInWindow = classCounts.HeldInWindow,
                    InscriptionsInWindow = classCounts.InscriptionsInWindow,
                    CapacityInWindow = classCounts.CapacityInWindow,
                    OccupancyRate = Percentage(classCounts.InscriptionsInWindow, classCounts.CapacityInWindow),
                    MostPopular = popular
                },
                Attendance = new AttendanceMetricsResponse
                {
                    Present = attendance.Present,
                    Absent = attendance.Absent,
                    NotRecorded = attendance.NotRecorded,
                    AttendanceRate = Percentage(attendance.Present, marked),
                    RecordedRate = Percentage(marked, allEnrolments)
                },
                Trainers = trainers
            };
        }

        /// <summary>
        /// Buckets payments into gym-local calendar months, empty months included so the timeline
        /// stays continuous.
        /// </summary>
        private static List<MonthlyRevenueResponse> GroupByGymMonth(
            List<PaymentPoint> payments, DateTime firstMonthStart, DateTime gymNow)
        {
            var paid = payments
                .Where(p => IsRevenue(p.PaymentState))
                // Month boundaries are gym-local: 22:00 on the 31st in Buenos Aires is the 1st in
                // UTC, so grouping the raw value files it under the wrong month.
                .Select(p => new { Local = GymTime.ToGymTime(p.PaymentDate), p.Price })
                .GroupBy(p => new { p.Local.Year, p.Local.Month })
                .ToDictionary(
                    g => (g.Key.Year, g.Key.Month),
                    g => (Amount: g.Sum(p => p.Price), Count: g.Count()));

            var months = new List<MonthlyRevenueResponse>();
            for (var month = firstMonthStart; month <= gymNow; month = month.AddMonths(1))
            {
                paid.TryGetValue((month.Year, month.Month), out var bucket);
                months.Add(new MonthlyRevenueResponse
                {
                    Year = month.Year,
                    Month = month.Month,
                    Label = month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Amount = bucket.Amount,
                    PaymentCount = bucket.Count
                });
            }

            return months;
        }

        private static bool IsRevenue(string state)
            => RevenueStates.Contains(state, StringComparer.OrdinalIgnoreCase);

        private static double Percentage(decimal part, decimal whole)
            => whole == 0 ? 0 : Math.Round((double)(part * 100 / whole), 1);
    }
}
