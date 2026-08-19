namespace GymManagement.Application.Common
{
    /// <summary>
    /// The gym's local wall-clock time. Class schedules are stored without a zone and must be
    /// compared against <see cref="Now"/>; everything else is a real instant on <c>DateTime.UtcNow</c>.
    /// </summary>
    public static class GymTime
    {
        private static readonly TimeZoneInfo Zone =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        public static DateTime Today => Now.Date;

        /// <summary>A UTC instant (payment date, expiry) as the gym's wall clock.</summary>
        public static DateTime ToGymTime(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        /// <summary>A gym wall-clock value as a UTC instant, for filtering UTC-stored columns.</summary>
        public static DateTime ToUtc(DateTime gymLocal)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(gymLocal, DateTimeKind.Unspecified), Zone);
    }
}
