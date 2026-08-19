using System.Globalization;
using System.Text;

namespace GymManagement.Application.Common
{
    public static class TextFormatter
    {
        /// <summary>Title-cases a name, capitalizing after spaces, hyphens and apostrophes.</summary>
        public static string ToTitleCase(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name ?? string.Empty;

            var normalized = string.Join(' ', name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries));
            var builder = new StringBuilder(normalized.Length);
            bool capitalizeNext = true;

            foreach (char c in normalized)
            {
                if (capitalizeNext && char.IsLetter(c))
                {
                    builder.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                    capitalizeNext = false;
                }
                else
                {
                    builder.Append(char.ToLower(c, CultureInfo.InvariantCulture));
                }

                capitalizeNext = c is ' ' or '-' or '\'';
            }

            return builder.ToString();
        }
    }
}
