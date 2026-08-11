namespace GymManagement.Application.Common
{
    /// <summary>
    /// The gym's local wall-clock time.
    /// <para>
    /// Class schedules (<c>GymClass.Schedule</c>, <c>GymClassSchedule.TimeOfDay</c>) are stored
    /// without a time zone and mean local time at the gym, so they must be compared against
    /// <see cref="Now"/> — against <c>DateTime.UtcNow</c> they are wrong by the gym's offset.
    /// Everything else (membership expiries, payment dates, tokens) is a real instant and stays
    /// on <c>DateTime.UtcNow</c>.
    /// </para>
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
