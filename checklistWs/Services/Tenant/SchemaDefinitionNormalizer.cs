using System.Globalization;
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
            if (string.Equals(NormalizeInOr(a), NormalizeInOr(e), StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(NormalizeClosedRange(a), NormalizeClosedRange(e), StringComparison.OrdinalIgnoreCase);
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
            return NormalizeEmbeddedClosedRanges(NormalizeEmbeddedInOr(compact));
        }

        private static string NormalizeEmbeddedClosedRanges(string value)
        {
            string normalized = Regex.Replace(
                value,
                @"(?<prefix>^|OR|AND)(?<column>[A-Z](?:(?!AND|OR)[A-Z0-9_])*)BETWEEN(?<lower>[0-9]+(?:\.[0-9]+)?)AND(?<upper>[0-9]+(?:\.[0-9]+)?)",
                match => match.Groups["prefix"].Value + ClosedRange.Create(match.Groups["column"].Value, match.Groups["lower"].Value, match.Groups["upper"].Value).ToCanonical(),
                RegexOptions.CultureInvariant);

            normalized = Regex.Replace(
                normalized,
                @"(?<prefix>^|OR|AND)(?<column>[A-Z](?:(?!AND|OR)[A-Z0-9_])*)>=(?<lower>[0-9]+(?:\.[0-9]+)?)AND\k<column><=(?<upper>[0-9]+(?:\.[0-9]+)?)",
                match => match.Groups["prefix"].Value + ClosedRange.Create(match.Groups["column"].Value, match.Groups["lower"].Value, match.Groups["upper"].Value).ToCanonical(),
                RegexOptions.CultureInvariant);

            return Regex.Replace(
                normalized,
                @"(?<prefix>^|OR|AND)(?<column>[A-Z](?:(?!AND|OR)[A-Z0-9_])*)<=(?<upper>[0-9]+(?:\.[0-9]+)?)AND\k<column>>=(?<lower>[0-9]+(?:\.[0-9]+)?)",
                match => match.Groups["prefix"].Value + ClosedRange.Create(match.Groups["column"].Value, match.Groups["lower"].Value, match.Groups["upper"].Value).ToCanonical(),
                RegexOptions.CultureInvariant);
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

        private static string NormalizeClosedRange(string value)
        {
            ClosedRange? between = TryParseBetween(value);
            if (between.HasValue) return between.Value.ToCanonical();

            ClosedRange? inclusiveRange = TryParseInclusiveRange(value);
            return inclusiveRange.HasValue ? inclusiveRange.Value.ToCanonical() : value;
        }

        private static ClosedRange? TryParseBetween(string value)
        {
            Match match = Regex.Match(
                value,
                @"^(?<column>[A-Z][A-Z0-9_]*)BETWEEN(?<lower>[0-9]+(?:\.[0-9]+)?)AND(?<upper>[0-9]+(?:\.[0-9]+)?)$",
                RegexOptions.CultureInvariant);

            return match.Success
                ? ClosedRange.Create(match.Groups["column"].Value, match.Groups["lower"].Value, match.Groups["upper"].Value)
                : null;
        }

        private static ClosedRange? TryParseInclusiveRange(string value)
        {
            Match terms = Regex.Match(
                value,
                @"^(?<first>[A-Z][A-Z0-9_]*(?:>=|<=)[0-9]+(?:\.[0-9]+)?)AND(?<second>[A-Z][A-Z0-9_]*(?:>=|<=)[0-9]+(?:\.[0-9]+)?)$",
                RegexOptions.CultureInvariant);
            if (!terms.Success) return null;

            Bound? first = TryParseBound(terms.Groups["first"].Value);
            Bound? second = TryParseBound(terms.Groups["second"].Value);
            if (!first.HasValue || !second.HasValue) return null;
            if (!string.Equals(first.Value.Column, second.Value.Column, StringComparison.Ordinal)) return null;

            string? lower = first.Value.Kind == BoundKind.Lower ? first.Value.Value : second.Value.Kind == BoundKind.Lower ? second.Value.Value : null;
            string? upper = first.Value.Kind == BoundKind.Upper ? first.Value.Value : second.Value.Kind == BoundKind.Upper ? second.Value.Value : null;

            return lower != null && upper != null
                ? ClosedRange.Create(first.Value.Column, lower, upper)
                : null;
        }

        private static Bound? TryParseBound(string value)
        {
            Match match = Regex.Match(
                value,
                @"^(?<column>[A-Z][A-Z0-9_]*)(?<operator>>=|<=)(?<value>[0-9]+(?:\.[0-9]+)?)$",
                RegexOptions.CultureInvariant);

            if (!match.Success) return null;

            BoundKind kind = match.Groups["operator"].Value == ">=" ? BoundKind.Lower : BoundKind.Upper;
            return new Bound(match.Groups["column"].Value, kind, NormalizeNumber(match.Groups["value"].Value));
        }

        private static string NormalizeNumber(string value) => decimal.Parse(value, CultureInfo.InvariantCulture).ToString("G29", CultureInfo.InvariantCulture);

        private readonly record struct ClosedRange(string Column, string Lower, string Upper)
        {
            public static ClosedRange Create(string column, string lower, string upper) => new(column, NormalizeNumber(lower), NormalizeNumber(upper));
            public string ToCanonical() => $"RANGE:{Column}:{Lower}:{Upper}";
        }

        private readonly record struct Bound(string Column, BoundKind Kind, string Value);

        private enum BoundKind
        {
            Lower,
            Upper
        }
    }
}
