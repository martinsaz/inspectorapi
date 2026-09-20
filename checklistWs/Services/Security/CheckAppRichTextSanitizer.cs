using System.Text.RegularExpressions;

namespace checklistWs.Services.Security
{
    public static class CheckAppRichTextSanitizer
    {
        private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "h2", "h3", "blockquote", "code", "pre", "a"
        };

        public static string Sanitize(string? value)
        {
            string html = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            html = html.Replace("\0", string.Empty);
            html = Regex.Replace(html, "<!--[\\s\\S]*?-->", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<(script|style|iframe|object|embed|form|input|button|textarea|select)[\\s\\S]*?</\\1>", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<[^>]+>", match => SanitizeAllowedTag(match.Value), RegexOptions.IgnoreCase);
            return html.Trim();
        }

        private static string SanitizeAllowedTag(string rawTag)
        {
            Match nameMatch = Regex.Match(rawTag, @"^<\s*/?\s*([a-z0-9]+)", RegexOptions.IgnoreCase);
            if (!nameMatch.Success)
            {
                return string.Empty;
            }

            string tagName = nameMatch.Groups[1].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(tagName))
            {
                return string.Empty;
            }

            bool isClosing = rawTag.Contains("</", StringComparison.Ordinal);
            bool selfClosing = rawTag.EndsWith("/>", StringComparison.Ordinal);
            if (isClosing)
            {
                return $"</{tagName}>";
            }

            if (!string.Equals(tagName, "a", StringComparison.OrdinalIgnoreCase))
            {
                return selfClosing ? $"<{tagName} />" : $"<{tagName}>";
            }

            Match hrefMatch = Regex.Match(rawTag, "\\s+href\\s*=\\s*(['\"])(.*?)\\1", RegexOptions.IgnoreCase);
            if (!hrefMatch.Success)
            {
                return "<a>";
            }

            string href = hrefMatch.Groups[2].Value.Trim();
            bool allowedHref = href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

            if (!allowedHref)
            {
                return "<a>";
            }

            string escapedHref = href.Replace("\"", "&quot;");
            return $"<a href=\"{escapedHref}\" target=\"_blank\" rel=\"noopener noreferrer\">";
        }
    }
}
