using System.Net;

namespace GymManagement.Application.Common
{
    /// <summary>
    /// HTML bodies for the gym's transactional emails, in Spanish. Kept out of the services so the
    /// wording lives in one place.
    /// </summary>
    public static class EmailTemplates
    {
        private const string ContainerStyle =
            "font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; " +
            "border: 1px solid #e0e0e0; border-radius: 10px;";

        public const string ClassCancelledSubject = "Clase cancelada - Gym Management";
        public const string ClassRescheduledSubject = "Cambio de horario en tu clase - Gym Management";

        /// <summary>Sent to enrolled clients when a class — or its whole schedule — is cancelled.</summary>
        public static string ClassCancelled(string clientName, string className, DateTime schedule) => $@"
            <div style='{ContainerStyle}'>
                <h2 style='color: #2b2b2b;'>Tu clase fue cancelada</h2>
                <p>Hola <strong>{Escape(clientName)}</strong>,</p>
                <p>Lamentamos informarte que la siguiente clase fue cancelada:</p>
                <p style='margin: 20px 0; padding: 15px; background-color: #f7f7f7; border-radius: 5px;'>
                    <strong>{Escape(className)}</strong><br />
                    {FormatSchedule(schedule)}
                </p>
                <p>Tu inscripción fue dada de baja automáticamente. Podés reservar otra clase desde tu cuenta cuando quieras.</p>
                <p style='color: #666; font-size: 13px;'>Disculpá las molestias.</p>
            </div>";

        /// <summary>Sent when an enrolled class moves to a different date or time.</summary>
        public static string ClassRescheduled(
            string clientName, string className, DateTime oldSchedule, DateTime newSchedule) => $@"
            <div style='{ContainerStyle}'>
                <h2 style='color: #2b2b2b;'>Tu clase cambió de horario</h2>
                <p>Hola <strong>{Escape(clientName)}</strong>,</p>
                <p>La clase <strong>{Escape(className)}</strong> en la que estás inscripto cambió de horario:</p>
                <p style='margin: 20px 0; padding: 15px; background-color: #f7f7f7; border-radius: 5px;'>
                    <span style='color: #999; text-decoration: line-through;'>{FormatSchedule(oldSchedule)}</span><br />
                    <strong style='color: #2b2b2b;'>{FormatSchedule(newSchedule)}</strong>
                </p>
                <p>Tu inscripción sigue activa. Si el nuevo horario no te sirve, podés darte de baja desde tu cuenta.</p>
            </div>";

        /// <summary>Class times are gym-local wall clock (see <see cref="GymTime"/>), so no conversion.</summary>
        private static string FormatSchedule(DateTime schedule) =>
            schedule.ToString("dddd dd/MM/yyyy 'a las' HH:mm", new System.Globalization.CultureInfo("es-AR"));

        /// <summary>Names are user input landing in HTML, so they are encoded.</summary>
        private static string Escape(string value) => WebUtility.HtmlEncode(value);
    }
}
