using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public static class SchemaDefinitionNormalizer
    {
        public static bool Same(string? actual, string? expected)
        {
            if (string.IsNullOrWhiteSpace(actual) && string.IsNullOrWhiteSpace(expected)) return true;
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected)) return false;
            string a = Normalize(actual);
            string e = Normalize(expected);
            if (string.Equals(a, e, StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(NormalizeInOr(a), NormalizeInOr(e), StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            string upper = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (upper.StartsWith("CHECK", StringComparison.Ordinal))
            {
                upper = upper[5..];
            }

            upper = upper.Replace("N'", "'", StringComparison.Ordinal);
            string compact = new string(upper.Where(ch => !char.IsWhiteSpace(ch) && ch != '[' && ch != ']' && ch != '(' && ch != ')').ToArray());
            return NormalizeEmbeddedInOr(compact);
        }

        private static string NormalizeEmbeddedInOr(string value)
        {
            string normalized = Regex.Replace(
                value,
                @"(?<prefix>^|OR|AND)(?<column>[A-Z](?:(?!AND|OR)[A-Z0-9_])*)IN(?<values>(?:'[^']+'|[0-9.]+)(?:,(?:'[^']+'|[0-9.]+))*)",
                match => $"{match.Groups["prefix"].Value}INSET:{match.Groups["column"].Value}:{SortValues(match.Groups["values"].Value)}",
                RegexOptions.CultureInvariant);

            return Regex.Replace(
                normalized,
                @"(?<column>[A-Z0-9_]+)=(?<value>'[^']+'|[0-9.]+)(?:OR\k<column>=(?<value>'[^']+'|[0-9.]+))+",
                match => $"INSET:{match.Groups["column"].Value}:{SortValues(string.Join(",", match.Groups["value"].Captures.Select(capture => capture.Value)))}",
                RegexOptions.CultureInvariant);
        }

        private static string SortValues(string values)
        {
            return string.Join("|", values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).OrderBy(v => v, StringComparer.Ordinal));
        }

        private static string NormalizeInOr(string value)
        {
            Match inMatch = Regex.Match(value, @"^(?<column>[A-Z0-9_]+)IN(?<values>.+)$", RegexOptions.CultureInvariant);
            if (inMatch.Success)
            {
                string values = string.Join("|", inMatch.Groups["values"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).OrderBy(v => v, StringComparer.Ordinal));
                return $"INSET:{inMatch.Groups["column"].Value}:{values}";
            }

            string[] orTerms = value.Split("OR", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (orTerms.Length > 1)
            {
                List<(string Column, string Value)> equality = new();
                foreach (string term in orTerms)
                {
                    Match eq = Regex.Match(term, @"^(?<column>[A-Z0-9_]+)=(?<value>.+)$", RegexOptions.CultureInvariant);
                    if (!eq.Success)
                    {
                        return value;
                    }

                    equality.Add((eq.Groups["column"].Value, eq.Groups["value"].Value));
                }

                if (equality.Select(x => x.Column).Distinct(StringComparer.Ordinal).Count() == 1)
                {
                    return $"INSET:{equality[0].Column}:{string.Join("|", equality.Select(x => x.Value).OrderBy(v => v, StringComparer.Ordinal))}";
                }
            }

            return value;
        }
    }
}
